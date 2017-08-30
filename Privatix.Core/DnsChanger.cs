using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace Privatix.Core
{
    public class DnsChanger : MarshalByRefObject
    {
        DnsChanger()
        {

        }


        private static DnsChanger _instance = null;
        public static DnsChanger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DnsChanger();
                }

                return _instance;
            }
        }


        private bool _enabled = false;
        private Dictionary<string, IPAddressCollection> _savedDns = new Dictionary<string, IPAddressCollection>();
        private object _lockObj = new object();


        public void EnableDnsLeakGuard(object obj)
        {
            try
            {
                lock (_lockObj)
                {
                    if (!_enabled)
                    {
                        FlushDns();

                        _savedDns.Clear();
                        var ifsList = NetworkInterface.GetAllNetworkInterfaces();
                        foreach (var networkInterface in ifsList)
                        {
                            try
                            {
                                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                                {
                                    continue;
                                }
                                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                                {
                                    continue;
                                }
                                if (networkInterface.Name.Contains(Config.RasEntryName))
                                {
                                    continue;
                                }

                                var ipProps = networkInterface.GetIPProperties();
                                if (ipProps.GetIPv4Properties().IsDhcpEnabled)
                                {
                                    _savedDns[networkInterface.Name] = null;
                                }
                                else
                                {
                                    _savedDns[networkInterface.Name] = ipProps.DnsAddresses;
                                }

                                _enabled |= SetDns(networkInterface.Name, Config.DefaultDnsServer);
                                AddDns(networkInterface.Name, Config.SecondDnsServer);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error(ex.Message, ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        public void DisableDnsLeakGuard(object obj)
        {
            try
            {
                lock (_lockObj)
                {
                    if (_enabled)
                    {
                        foreach (var ifName in _savedDns.Keys)
                        {
                            var dnsList = _savedDns[ifName];

                            if (dnsList == null || dnsList.Count <= 0)
                            {
                                SetDhcpDns(ifName);
                            }
                            else
                            {
                                bool isFirst = true;
                                foreach (var dns in dnsList)
                                {
                                    if (isFirst)
                                    {
                                        SetDns(ifName, dns.ToString());
                                        isFirst = false;
                                    }
                                    else
                                    {
                                        AddDns(ifName, dns.ToString());
                                    }
                                }
                            }
                        }

                        FlushDns();

                        _savedDns.Clear();
                        _enabled = false;                    
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        private bool FlushDns()
        {
            var dirSys = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            var dirWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(dirSys, "ipconfig.exe"),
                Arguments = "/flushdns",
                WorkingDirectory = dirWin,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = new System.Diagnostics.Process { StartInfo = psi };
            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private bool SetDns(string ifName, string newDns)
        {
            var dirSys = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            var dirWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var args = string.Format("interface ip set dns \"{0}\" static {1} both", ifName, newDns);

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(dirSys, "netsh.exe"),
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

        private bool AddDns(string ifName, string newDns)
        {
            var dirSys = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            var dirWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var args = string.Format("interface ip add dns \"{0}\" {1}", ifName, newDns);

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(dirSys, "netsh.exe"),
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

        private bool SetDhcpDns(string ifName)
        {
            if (!_enabled || string.IsNullOrEmpty(ifName))
                return false;

            var dirSys = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            var dirWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var args = string.Format("interface ip set dns \"{0}\" dhcp", ifName);

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(dirSys, "netsh.exe"),
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

        public override object InitializeLifetimeService()
        {
            return (null);
        }
    }
}
