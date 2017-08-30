using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;


namespace Privatix.Core
{
    public class Guard : MarshalByRefObject
    {
        Guard()
        {
            
        }

        private static Guard _instance = null;
        public static Guard Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Guard();
                }

                return _instance;
            }
        }

        private object _guardLock = new object();
        private List<RouteRow> _addedRoutes = new List<RouteRow>();
        private List<RouteRow> _deletedRoutes = new List<RouteRow>();
        private List<RouteRow> _restoredRoutes = new List<RouteRow>();

        private GuardStatus _status = GuardStatus.Disabled;
        public GuardStatus Status
        {
            get
            {
                try
                {
                    return _status;
                }
                catch 
                {
                    return GuardStatus.Error;
                }
            }
        }

        private IPAddress _defaultDns = null;
        public IPAddress DefaultDns
        {
            get
            {
                if (_defaultDns == null)
                {
                    _defaultDns = IPAddress.Parse(Config.DefaultDnsServer);
                }

                return _defaultDns;
            }
        }

        private NetworkAdapterInfo _defaultNetwork;
        public NetworkAdapterInfo DefaultNetwork
        {
            get
            {
                if (_defaultNetwork.IfIndex == 0)
                {
                    _defaultNetwork = GetDefaultNetwork();
                }

                return _defaultNetwork;
            }
        }

        private bool _guardEnabled = false;
        public bool IsGuardEnabled
        {
            get
            {
                try
                {
                    lock (_guardLock)
                    {
                        return _guardEnabled;
                    }
                }
                catch 
                {
                    return false;
                }
            }
        }

        public bool NeedShow
        {
            get
            {
                lock (_guardLock)
                {
                    bool result = _guardEnabled && (_status == GuardStatus.Connected || _status == GuardStatus.Reconnecting || _status == GuardStatus.Error);
                    if (result)
                    {
                        Guard.Instance.CheckGuardWork();
                        _status = GuardStatus.Working;
                    }

                    return result;
                }
            }
        }

        public bool EnableGuard()
        {
            lock (_guardLock)
            {
                try
                {
                    if (_guardEnabled)
                    {
                        Logger.Log.Info("Guard already enabled");
                        return true;
                    }

                    if (_status == GuardStatus.Disabling)
                    {
                        Logger.Log.Info("Guard disabling...");
                        return false;
                    }

                    _status = GuardStatus.Disabled;

                    _restoredRoutes.Clear();
                    _defaultNetwork = GetDefaultNetwork();

                    bool res = LockNetwork();

                    if (res)
                    {
                        Logger.Log.Info("Guard enabled");
                        _guardEnabled = true;
                        _status = GuardStatus.Initialized;
                    }
                    else
                    {
                        RestoreRoutes();
                        _status = GuardStatus.Disabled;
                    }

                    return res;
                }
                catch (Exception ex)
                {
                    Logger.Log.ErrorFormat("EnableGuard error " + ex.Message + " \n" + ex.StackTrace);
                    _status = GuardStatus.Error;
                    return false;
                }

            }
        }

        public bool LockNetwork()
        {
            //Logger.Log.Info("Route table before lock:\n" + RouteTable.StringRouteTable());
            bool res = DeleteRoute(IPAddress.Any, IPAddress.Any);
            //Logger.Log.Info("Route table after delete:\n" + RouteTable.StringRouteTable());
            res &= AddRoute(IPAddress.Any, IPAddress.Any, _defaultNetwork.Address, _defaultNetwork.IfIndex, 2);
            //Logger.Log.Info("Route table after add:\n" + RouteTable.StringRouteTable());

            return res;
        }

        public void DisableGuard()
        {
            lock (_guardLock)
            {

                if (!IsDefaultNetworkConnected())
                {
                    Logger.Log.Info("Guard disabling...");
                    _status = GuardStatus.Disabling;
                    return;
                }

                if (!_guardEnabled)
                {
                    Logger.Log.Info("Guard not enabled");
                    return;
                }

                RestoreRoutes();

                _guardEnabled = false;
                _restoredRoutes.Clear();
                _status = GuardStatus.Disabled;
                Logger.Log.Info("Guard disabled");
            }
        }

        public bool AddRoute(IPAddress dest, IPAddress mask, IPAddress gateway, uint ifIndex = 0, uint metric = 1)
        {
            if (ifIndex == 0)
            {
                ifIndex = DefaultNetwork.IfIndex;
            }

            RouteRow row = new RouteRow();
            row.Destination = dest;
            row.Mask = mask;
            row.Gateway = gateway;
            row.IfIndex = ifIndex;
            row.Metric = metric;

            //foreach (RouteRow r in _addedRoutes)
            //{
            //    if (r.Destination.Equals(row.Destination)
            //        && r.Mask.Equals(row.Mask)
            //        && r.Gateway.Equals(row.Gateway))
            //    {
            //        Logger.Log.WarnFormat("Already added {0} MASK {1} {2}", row.Destination, row.Mask, row.Gateway);
            //        return true;
            //    }
            //}

            if (RouteTable.Create(dest, mask, gateway, ifIndex, metric))
            {
                _addedRoutes.Add(row);

                return true;
            }
            else if (RouteTable.LastError == 87)
            {
                if (RouteTable.AppendRoute(dest, mask, gateway, ifIndex, metric))
                {
                    _addedRoutes.Add(row);

                    return true;
                }
            }

            return false;
        }

        public bool DeleteRoute(IPAddress dest, IPAddress mask = null, IPAddress gateway = null, uint ifIndex = 0)
        {
            try
            {
                var routes = RouteTable.Read();
                foreach (var r in routes)
                {
                    if (!dest.Equals(r.Destination))
                        continue;

                    if (mask != null && !mask.Equals(r.Mask))
                        continue;

                    if (gateway != null && !gateway.Equals(r.Gateway))
                        continue;

                    if (ifIndex != 0 && ifIndex != r.IfIndex)
                        continue;

                    _deletedRoutes.Add(r);
                }

                return RouteTable.Delete(dest, mask, gateway, ifIndex);
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("DeleteRoute error " + ex.Message + " \n" + ex.StackTrace);
                return false;
            }
        }

        public bool RestoreRoutes()
        {
            bool res = true;

            foreach (var r in _addedRoutes)
            {
                res &= RouteTable.Delete(r.Destination, r.Mask, r.Gateway, r.IfIndex);
            }
            _addedRoutes.Clear();

            foreach (var r in _deletedRoutes)
            {
                res = RouteTable.Create(r.Destination, r.Mask, r.Gateway, r.IfIndex, r.Metric);
            }
            _deletedRoutes.Clear();

            return res;
        }

        private NetworkAdapterInfo GetDefaultNetwork()
        {
            NetworkAdapterInfo defAdapter = new NetworkAdapterInfo();

            try
            {
                var routes = RouteTable.Read();
                var ifsList = NetworkInterface.GetAllNetworkInterfaces();
                var defRoute = routes.FirstOrDefault(r => r.Destination.Equals(IPAddress.Any) && r.Mask.Equals(IPAddress.Any));
                if (defRoute.Gateway.Equals(IPAddress.Any))
                {
                    defRoute = routes.OrderBy(m => m.Metric).FirstOrDefault(r => r.IfIndex != NetworkInterface.LoopbackInterfaceIndex);
                }
                if (defRoute.Gateway.Equals(IPAddress.Any))
                {
                    throw new Exception("Default route not found");
                }

                var ifs = ifsList.FirstOrDefault(i => i.GetIPProperties().GetIPv4Properties().Index == defRoute.IfIndex);
                if (ifs == null)
                {
                    throw new Exception("Default network interface not found");
                }

                var ipProps = ifs.GetIPProperties();
                var address = ipProps.UnicastAddresses.First(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                defAdapter.Address = address.Address;
                defAdapter.Subnet = address.IPv4Mask;
                defAdapter.Gateway = ipProps.GatewayAddresses.First(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Address;
                defAdapter.IfIndex = (uint)ipProps.GetIPv4Properties().Index;
                if (ipProps.DnsAddresses.Count > 0)
                {
                    defAdapter.DNS = new IPAddress[ipProps.DnsAddresses.Count];
                    ipProps.DnsAddresses.CopyTo(defAdapter.DNS, 0);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Default network not found", ex);
            }

            return defAdapter;
        }

        public void OnConnected()
        {
            if (_guardEnabled)
            {
                _status = GuardStatus.Connected;
            }
        }

        public void OnDisconnected()
        {
            try
            {
                if (_guardEnabled)
                {
                    DisableGuard();
                }
            }
            catch { }
        }

        public static bool IsLocalAddress(IPAddress ipAddr)
        {
            uint dnsAddr = ipAddr.GetAddress();
            IPAddress localNet = IPAddress.Parse("10.0.0.0");
            IPAddress localMask = IPAddress.Parse("255.0.0.0");
            uint localAddr = localNet.GetAddress();
            uint maskAddr = localMask.GetAddress();
            if ((dnsAddr & maskAddr) == (localAddr & maskAddr))
            {
                return true;
            }

            localNet = IPAddress.Parse("172.16.0.0");
            localMask = IPAddress.Parse("255.240.0.0");
            localAddr = localNet.GetAddress();
            maskAddr = localMask.GetAddress();
            if ((dnsAddr & maskAddr) == (localAddr & maskAddr))
            {
                return true;
            }

            localNet = IPAddress.Parse("192.168.0.0");
            localMask = IPAddress.Parse("255.255.0.0");
            localAddr = localNet.GetAddress();
            maskAddr = localMask.GetAddress();
            if ((dnsAddr & maskAddr) == (localAddr & maskAddr))
            {
                return true;
            }

            return false;
        }

        public bool AddDefaultNetworkDns()
        {
            if (_defaultNetwork.DNS == null)
            {
                return true;
            }

            bool res = true;

            foreach (IPAddress dns in _defaultNetwork.DNS)
            {
                if (Guard.IsLocalAddress(dns))
                {
                    continue;
                }

                res &= AddRoute(dns, IPAddress.None, _defaultNetwork.Gateway, _defaultNetwork.IfIndex, 5);
            }

            return res;
        }

        public bool DeleteDefaultNetworkDns()
        {
            if (_defaultNetwork.DNS == null)
            {
                return true;
            }

            bool res = true;

            foreach (IPAddress dns in _defaultNetwork.DNS)
            {
                if (Guard.IsLocalAddress(dns))
                {
                    continue;
                }

                foreach (var r in _addedRoutes)
                {
                    if (!r.Destination.Equals(dns))
                    {
                        continue;
                    }
                    if (!r.Mask.Equals(IPAddress.None))
                    {
                        continue;
                    }
                    if (!r.Gateway.Equals(_defaultNetwork.Gateway))
                    {
                        continue;
                    }
                    if (r.IfIndex != _defaultNetwork.IfIndex)
                    {
                        continue;
                    }

                    res = RouteTable.Delete(r.Destination, r.Mask, r.Gateway, r.IfIndex);
                    if (!res)
                    {
                        Logger.Log.Error("Delete Route error: " + RouteTable.LastError.ToString());
                    }

                    _addedRoutes.Remove(r);
                    break;
                }
            }

            return res;
        }

        public bool AddDefaultDns()
        {
            return AddRoute(DefaultDns, IPAddress.None, _defaultNetwork.Gateway, _defaultNetwork.IfIndex, 5);
        }

        public bool DeleteDefaultDns()
        {
            bool res = false;

            foreach (var r in _addedRoutes)
            {
                if (!r.Destination.Equals(DefaultDns))
                {
                    continue;
                }
                if (!r.Mask.Equals(IPAddress.None))
                {
                    continue;
                }
                if (!r.Gateway.Equals(_defaultNetwork.Gateway))
                {
                    continue;
                }
                if (r.IfIndex != _defaultNetwork.IfIndex)
                {
                    continue;
                }

                res = RouteTable.Delete(r.Destination, r.Mask, r.Gateway, r.IfIndex);
                if (!res)
                {
                    Logger.Log.Error("Delete Route error: " + RouteTable.LastError.ToString());
                }

                _addedRoutes.Remove(r);
                break;
            }

            return res;
        }

        public void ClearServerIpRoutes(IPAddress serverIp)
        {
            RouteTable.Delete(serverIp, IPAddress.None, null, 0);
        }

        public bool Check()
        {
            lock (_guardLock)
            {
                if (!_guardEnabled)
                {
                    Logger.Log.Warn("Guard not enabled");
                    return true;
                }

                if (_addedRoutes.Count == 0 && _deletedRoutes.Count == 0)
                {
                    Logger.Log.Warn("Guard enabled, but no routes");
                    return true;
                }

                List<RouteRow> addedRoutesCopy = new List<RouteRow>();
                foreach (var r in _addedRoutes)
                {
                    if (r.Destination.Equals(IPAddress.Any)
                        && r.Mask.Equals(IPAddress.Any))
                    {
                        addedRoutesCopy.Add(r);
                    }
                }

                try
                {
                    var table = RouteTable.Read();

                    foreach (var route in table)
                    {
                        foreach (var deletedRoute in _deletedRoutes)
                        {
                            if (route.Destination.Equals(deletedRoute.Destination)
                                && route.Mask.Equals(deletedRoute.Mask)
                                && route.Gateway.Equals(deletedRoute.Gateway)
                                && route.IfIndex == deletedRoute.IfIndex)
                            {
                                Logger.Log.WarnFormat("Deleted route found {0} {1} {2} {3}", route.Destination.ToString(), route.Mask.ToString(), route.Gateway.ToString(), route.IfIndex);
                                _restoredRoutes.Add(route);
                            }
                        }

                        foreach (var addedRoute in addedRoutesCopy)
                        {
                            if (route.Destination.Equals(addedRoute.Destination)
                                && route.Mask.Equals(addedRoute.Mask)
                                && route.Gateway.Equals(addedRoute.Gateway)
                                && route.IfIndex == addedRoute.IfIndex)
                            {
                                addedRoutesCopy.Remove(addedRoute);
                                break;
                            }
                        }
                    }

                    if (addedRoutesCopy.Count > 0)
                    {
                        foreach (var addedRoute in addedRoutesCopy)
                        {

                            Logger.Log.WarnFormat("Added route not found {0} {1} {2} {3}", addedRoute.Destination.ToString(), addedRoute.Mask.ToString(), addedRoute.Gateway.ToString(), addedRoute.IfIndex);
                        }

                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                    return false;
                }
            }
        }

        public void CheckGuardWork()
        {
            try
            {
                if (!_guardEnabled && _status != GuardStatus.Disabling)
                    return;

                if (!IsDefaultNetworkConnected())
                {
                    Logger.Log.Info("Guard broken! Default network down.");
                    CloseActiveVpnConnection();
                    return;
                }

                if (_status == GuardStatus.Disabling)
                {
                    Logger.Log.Info("Guard broken! Disable it!");
                    DisableGuard();
                    return;
                }

                if (_status == GuardStatus.Error || !Check())
                {
                    Logger.Log.InfoFormat("Guard broken! Reinit. status = {0}", _status);
                    if (CloseActiveVpnConnection())
                    {
                        Logger.Log.Info("Guard broken! Active connection closed");
                        return;
                    }
                    //Logger.Log.Info("Route table before restore:\n" + RouteTable.StringRouteTable());
                    RestoreRoutes();
                    //Logger.Log.Info("Route table after restore:\n" + RouteTable.StringRouteTable());
                    if (!LockNetwork())
                    {
                        _status = GuardStatus.Error;
                    }
                    else
                    {
                        _status = GuardStatus.Connected;
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log.Error("Check guard error", ex);
                return;
            }
        }

        public void SetReconnected()
        {
            _status = GuardStatus.Reconnecting;
        }

        private bool IsDefaultNetworkConnected()
        {
            bool result = false;
            var ifList = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var i in ifList)
            {
                try
                {
                    var ipProps = i.GetIPProperties();
                    if (ipProps == null)
                    {
                        continue;
                    }
                    var ip4Props = ipProps.GetIPv4Properties();
                    if (ip4Props == null)
                    {
                        continue;
                    }

                    int index = ip4Props.Index;
                    if (index != _defaultNetwork.IfIndex)
                    {
                        continue;
                    }

                    result = i.OperationalStatus == OperationalStatus.Up;
                    break;
                }
                catch (Exception )
                {
                    //Logger.Log.Error(ex.Message, ex);
                }
            }

            return result;
        }

        private bool CloseActiveVpnConnection()
        {
            return SyncConnector.Instance.DoAnyDisconnect();
        }

        public override object InitializeLifetimeService() 
        { 
            return (null); 
        }
    }

    public enum GuardStatus
    {
        Disabled = 0,
        Initialized,
        Connected,
        Working,
        Reconnecting,
        Disabling,
        Error
    }

    [Serializable]
    public struct NetworkAdapterInfo
    {
        public IPAddress Address;
        public IPAddress Subnet;
        public IPAddress Gateway;
        public uint IfIndex;
        public IPAddress[] DNS;
    }
}
