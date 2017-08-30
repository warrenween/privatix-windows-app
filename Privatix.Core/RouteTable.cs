using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;


namespace Privatix.Core
{
    struct RouteRow
    {
        public IPAddress Destination;
        public IPAddress Mask;
        public IPAddress Gateway;
        public uint IfIndex;
        public uint Metric;
        public uint Type;
        public uint Proto;
        public uint Age;
    }

    static class RouteTable
    {
        #region Consts

        private const int NO_ERROR = 0;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        private const uint MIB_IPPROTO_NETMGMT = 3;

        private const uint MIB_IPROUTE_TYPE_DIRECT = 3;
        private const uint MIB_IPROUTE_TYPE_INDIRECT = 4;

        #endregion

        #region Types

        [ComVisible(false), StructLayout(LayoutKind.Sequential)]
        internal struct IPForwardTable
        {
            public uint Size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public IPFORWARDROW[] Table;
        };

        [ComVisible(false), StructLayout(LayoutKind.Sequential)]
        internal struct IPFORWARDROW
        {
            internal uint /*DWORD*/ dwForwardDest;
            internal uint /*DWORD*/ dwForwardMask;
            internal uint /*DWORD*/ dwForwardPolicy;
            internal uint /*DWORD*/ dwForwardNextHop;
            internal uint /*DWORD*/ dwForwardIfIndex;
            internal uint /*DWORD*/ dwForwardType;
            internal uint /*DWORD*/ dwForwardProto;
            internal uint /*DWORD*/ dwForwardAge;
            internal uint /*DWORD*/ dwForwardNextHopAS;
            internal uint /*DWORD*/ dwForwardMetric1;
            internal uint /*DWORD*/ dwForwardMetric2;
            internal uint /*DWORD*/ dwForwardMetric3;
            internal uint /*DWORD*/ dwForwardMetric4;
            internal uint /*DWORD*/ dwForwardMetric5;
        };

        #endregion

        #region NativeMethods

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        private extern static int GetIpForwardTable(IntPtr /*PMIB_IPFORWARDTABLE*/ pIpForwardTable, ref int /*PULONG*/ pdwSize, bool bOrder);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        private extern static int CreateIpForwardEntry(IntPtr /*PMIB_IPFORWARDROW*/ pRoute);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        private extern static int DeleteIpForwardEntry(IntPtr /*PMIB_IPFORWARDROW*/ pRoute);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        private extern static int SetIpForwardEntry(IntPtr /*PMIB_IPFORWARDROW*/ pRoute);

        #endregion


        private static int _lastError = 0;
        public static int LastError
        {
            get { return _lastError; }
        }


        private static IPForwardTable ReadIPForwardTable(IntPtr tablePtr)
        {
            var fwdTable = (IPForwardTable)Marshal.PtrToStructure(tablePtr, typeof(IPForwardTable));

            IPFORWARDROW[] table = new IPFORWARDROW[fwdTable.Size];
            IntPtr p = new IntPtr(tablePtr.ToInt64() + Marshal.SizeOf(fwdTable.Size));
            for (int i = 0; i < fwdTable.Size; ++i)
            {
                table[i] = (IPFORWARDROW)Marshal.PtrToStructure(p, typeof(IPFORWARDROW));
                p = new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(IPFORWARDROW)));
            }
            fwdTable.Table = table;

            return fwdTable;
        }

        public static IEnumerable<RouteRow> Read()
        {
            List<RouteRow> routes = new List<RouteRow>();
            var pRouteTable = IntPtr.Zero;
            int size = 0;
            var result = GetIpForwardTable(pRouteTable, ref size, true);
            if (result != ERROR_INSUFFICIENT_BUFFER)
            {
                _lastError = result;
                throw new Exception("GetIpForwardTable failed error: " + result.ToString());
            }

            pRouteTable = Marshal.AllocHGlobal(size);
            result = GetIpForwardTable(pRouteTable, ref size, true);
            if (result != NO_ERROR)
            {
                _lastError = result;
                Marshal.FreeHGlobal(pRouteTable);
                throw new Exception("GetIpForwardTable failed error: " + result.ToString());
            }

            var forwardTable = ReadIPForwardTable(pRouteTable);
            Marshal.FreeHGlobal(pRouteTable);

            for (int i = 0; i < forwardTable.Table.Length; ++i)
            {
                RouteRow row = new RouteRow();
                row.Destination = new IPAddress((long)forwardTable.Table[i].dwForwardDest);
                row.Mask = new IPAddress((long)forwardTable.Table[i].dwForwardMask);
                row.Gateway = new IPAddress((long)forwardTable.Table[i].dwForwardNextHop);
                row.IfIndex = forwardTable.Table[i].dwForwardIfIndex;
                row.Metric = forwardTable.Table[i].dwForwardMetric1;
                row.Type = forwardTable.Table[i].dwForwardType;
                row.Proto = forwardTable.Table[i].dwForwardProto;
                row.Age = forwardTable.Table[i].dwForwardAge;

                routes.Add(row);
            }

            return routes;
        }

        public static bool Create(IPAddress dest, IPAddress mask, IPAddress gateway, uint ifIndex, uint metric)
        {
            int result = 0;
            IPFORWARDROW forwardRow = new IPFORWARDROW();

            forwardRow.dwForwardDest = dest.GetAddress();
            forwardRow.dwForwardNextHop = gateway.GetAddress();
            forwardRow.dwForwardMask = mask.GetAddress();
            forwardRow.dwForwardIfIndex = ifIndex;
            forwardRow.dwForwardMetric1 = metric;
            forwardRow.dwForwardType = MIB_IPROUTE_TYPE_INDIRECT;
            forwardRow.dwForwardProto = MIB_IPPROTO_NETMGMT;
            forwardRow.dwForwardAge = 0;


            do
            {
                int size = Marshal.SizeOf(forwardRow);
                IntPtr pRoute = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(forwardRow, pRoute, true);

                result = CreateIpForwardEntry(pRoute);
                _lastError = result;

                Marshal.FreeHGlobal(pRoute);

                if (result != 160)
                    break;

                ++forwardRow.dwForwardMetric1;
            } while (forwardRow.dwForwardMetric1 < 2048);

            if (result != NO_ERROR)
            {
                Logger.Log.ErrorFormat("CreateIpForwardEntry dest={0} mask={1} gate={2} if={3} metric={4} error:{5}", dest.ToString(), mask.ToString(), gateway.ToString(), ifIndex, metric, result);
            }

            return result == NO_ERROR;
        }

        public static bool Delete(IPAddress dest, IPAddress mask = null, IPAddress gateway = null, uint ifIndex = 0)
        {
            bool ret = false;

            var pRouteTable = IntPtr.Zero;
            int size = 0;

            try
            {
                var result = GetIpForwardTable(pRouteTable, ref size, true);
                if (result != ERROR_INSUFFICIENT_BUFFER)
                {
                    _lastError = result;
                    return false;
                }

                pRouteTable = Marshal.AllocHGlobal(size);
                result = GetIpForwardTable(pRouteTable, ref size, true);
                if (result != NO_ERROR)
                {
                    _lastError = result;
                    return false;
                }

                var fwdTable = (IPForwardTable)Marshal.PtrToStructure(pRouteTable, typeof(IPForwardTable));
                IntPtr p = new IntPtr(pRouteTable.ToInt64() + Marshal.SizeOf(fwdTable.Size));
                for (int i = 0; i < fwdTable.Size; ++i, p = new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(IPFORWARDROW))))
                {
                    IPFORWARDROW row = (IPFORWARDROW)Marshal.PtrToStructure(p, typeof(IPFORWARDROW));

                    if (dest.GetAddress() != row.dwForwardDest)
                        continue;

                    if (mask != null && mask.GetAddress() != row.dwForwardMask)
                        continue;

                    if (gateway != null && gateway.GetAddress() != row.dwForwardNextHop)
                        continue;

                    if (ifIndex != 0 && ifIndex != row.dwForwardIfIndex)
                        continue;

                    result = DeleteIpForwardEntry(p);
                    if (result == NO_ERROR)
                    {
                        ret = true;
                    }
                    else
                    {
                        Logger.Log.ErrorFormat("DeleteIpForwardEntry dest={0} mask={1} gate={2} if={3} error:{5}", dest.ToString(), mask.ToString(), gateway.ToString(), ifIndex, result);
                    }
                }
            }
            finally
            {
                if (pRouteTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pRouteTable);
                }
            }

            return ret;
        }

        public static bool ChangeMetric(IPAddress dest, uint newMetric, IPAddress mask = null, IPAddress gateway = null, uint ifIndex = 0)
        {
            bool ret = false;

            var pRouteTable = IntPtr.Zero;
            var pNewRoute = IntPtr.Zero;
            int size = 0;

            try
            {
                var result = GetIpForwardTable(pRouteTable, ref size, true);
                if (result != ERROR_INSUFFICIENT_BUFFER)
                {
                    _lastError = result;
                    return false;
                }

                pRouteTable = Marshal.AllocHGlobal(size);
                result = GetIpForwardTable(pRouteTable, ref size, true);
                if (result != NO_ERROR)
                {
                    _lastError = result;
                    return false;
                }

                var fwdTable = (IPForwardTable)Marshal.PtrToStructure(pRouteTable, typeof(IPForwardTable));
                IntPtr p = new IntPtr(pRouteTable.ToInt64() + Marshal.SizeOf(fwdTable.Size));
                for (int i = 0; i < fwdTable.Size; ++i, p = new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(IPFORWARDROW))))
                {
                    IPFORWARDROW row = (IPFORWARDROW)Marshal.PtrToStructure(p, typeof(IPFORWARDROW));

                    if (dest.GetAddress() != row.dwForwardDest)
                        continue;

                    if (mask != null && mask.GetAddress() != row.dwForwardMask)
                        continue;

                    if (gateway != null && gateway.GetAddress() != row.dwForwardNextHop)
                        continue;

                    if (ifIndex != 0 && ifIndex != row.dwForwardIfIndex)
                        continue;

                    row.dwForwardMetric1 = newMetric;

                    if (pNewRoute != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pNewRoute);
                    }
                    pNewRoute = Marshal.AllocHGlobal(Marshal.SizeOf(row));
                    Marshal.StructureToPtr(row, pNewRoute, true);

                    result = DeleteIpForwardEntry(p);
                    if (result == NO_ERROR)
                    {
                        ret = true;
                    }
                }
            }
            finally
            {
                if (pRouteTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pRouteTable);
                }
                if (pNewRoute != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pNewRoute);
                }
            }

            return ret;
        }

        public static bool AppendRoute(IPAddress ip, IPAddress mask, IPAddress gate, uint ifIndex, uint metric)
        {
            var args = string.Format("add {0} mask {1} {2} metric {3} if {4} ", ip.ToString(), mask.ToString(), gate.ToString(), metric, ifIndex);
            return ExecuteRouter(args);
        }

        private static bool ExecuteRouter(string args)
        {
            var dirSys = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            var dirWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var psi = new ProcessStartInfo
            {
                FileName = dirSys + "route.exe",
                Arguments = args,
                WorkingDirectory = dirWin,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = new System.Diagnostics.Process { StartInfo = psi };
            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        //public static string StringRouteTable()
        //{
        //    StringBuilder result = new StringBuilder();
        //    var routes = Read();
        //    foreach (var r in routes)
        //    {
        //        result.AppendFormat("\t{0} MASK {1} {2} IF {3}\n", r.Destination.ToString(), r.Mask.ToString(), r.Gateway.ToString(), r.IfIndex);
        //    }

        //    return result.ToString();
        //}

        public static uint GetAddress(this IPAddress ipAddress)
        {
            if (ipAddress == null)
                return 0;

            var ipBytes = ipAddress.GetAddressBytes();
            uint ip = (uint)ipBytes [3] << 24;
            ip += (uint)ipBytes [2] << 16;
            ip += (uint)ipBytes [1] <<8;
            ip += (uint)ipBytes [0];

            return ip;
        }
    }
}
