using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using NativeWifi;


namespace Privatix.Core
{
    public class NetworkWatcher
    {
        ConnectedNetwork lastConnectedNetwork;
        WlanClient wlanClient = null;


        public NetworkWatcher()
        {

        }

        public void Init()
        {
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            TryGetWlanClient();

            lastConnectedNetwork = GetCurrentNetwork();
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Check();
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            Check();
        }

        private ConnectedNetwork GetCurrentNetwork()
        {
            try
            {
                var routes = RouteTable.Read();
                var defRoute = routes.OrderBy(r => r.Metric).FirstOrDefault(r => r.Destination.Equals(IPAddress.Any) && r.Mask.Equals(IPAddress.Any) && r.IfIndex != NetworkInterface.LoopbackInterfaceIndex);
                if (defRoute.Gateway.Equals(IPAddress.Any))
                {
                    throw new Exception("Default route not found");
                }

                var ifsList = NetworkInterface.GetAllNetworkInterfaces();
                var ifs = ifsList.FirstOrDefault(i => i.GetIPProperties().GetIPv4Properties().Index == defRoute.IfIndex);
                if (ifs == null)
                {
                    throw new Exception("Default network interface not found");
                }

                ConnectedNetwork connectedNetwork = new ConnectedNetwork(ifs.Name, ifs.Id, NetworkType.LAN);

                try
                {
                    if (wlanClient == null)
                    {
                        TryGetWlanClient();
                    }

                    if (wlanClient != null)
                    {
                        var wlanIf = wlanClient.Interfaces.FirstOrDefault(i => i.NetworkInterface.Id == ifs.Id);
                        if (wlanIf != null)
                        {
                            if (wlanIf.CurrentConnection.wlanSecurityAttributes.securityEnabled)
                            {
                                connectedNetwork.Type = NetworkType.SecuredWiFi;
                            }
                            else
                            {
                                connectedNetwork.Type = NetworkType.OpenWiFi;
                            }

                            connectedNetwork.Name = wlanIf.CurrentConnection.profileName;
                        }
                    }
                }
                catch
                {

                }

                if (connectedNetwork.Type == NetworkType.LAN)
                {
                    if (ifs.NetworkInterfaceType == NetworkInterfaceType.GenericModem)
                    {
                        connectedNetwork.Type = NetworkType.Mobile;
                    }
                    else if (ifs.Speed != 10000000 && ifs.Speed != 100000000 && ifs.Speed != 1000000000)
                    {
                        connectedNetwork.Type = NetworkType.Mobile;
                    }
                }

                return connectedNetwork;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return new ConnectedNetwork();
            }
        }

        public void Check()
        {
            var currentNetwork = GetCurrentNetwork();
            if (currentNetwork.Name == Config.RasEntryName)
            {
                return;
            }

            if (lastConnectedNetwork == null || currentNetwork.Equals(lastConnectedNetwork))
            {
                return;
            }

            Logger.Log.InfoFormat("Changed network: {0} {1} {2}", currentNetwork.Type.ToString(), currentNetwork.Name, currentNetwork.InterfaceNumber);

            if (currentNetwork.Type == NetworkType.OpenWiFi || currentNetwork.Type == NetworkType.Mobile)
            {
                GlobalEvents.SecureNetworkNotify(this);
            }            

            lastConnectedNetwork = currentNetwork;
        }

        private void TryGetWlanClient()
        {
            try
            {
                wlanClient = new WlanClient();
                foreach (var wlanIf in wlanClient.Interfaces)
                {
                    wlanIf.WlanConnectionNotification += wlanIf_WlanConnectionNotification;
                }
            }
            catch
            {
                wlanClient = null;
                //Logger.Log.Warn(ex.Message, ex);
            }
        }

        void wlanIf_WlanConnectionNotification(Wlan.WlanNotificationData notifyData, Wlan.WlanConnectionNotificationData connNotifyData)
        {
            Check();
        }

        enum NetworkType
        {
            None,
            LAN,
            OpenWiFi,
            SecuredWiFi,
            Mobile
        }

        class ConnectedNetwork : IEquatable<ConnectedNetwork>
        {
            public string Name;
            public string InterfaceNumber;
            public NetworkType Type;

            public ConnectedNetwork()
            {
                Name = "";
                InterfaceNumber = "0";
                Type = NetworkType.None;
            }

            public ConnectedNetwork(string name, string interfaceNumber, NetworkType type)
            {
                Name = name;
                InterfaceNumber = interfaceNumber;
                Type = type;
            }

            public bool Equals(ConnectedNetwork cn)
            {
                try
                {
                    if (InterfaceNumber.ToLower() != cn.InterfaceNumber.ToLower())
                    {
                        return false;
                    }

                    if (Type != cn.Type)
                    {
                        return false;
                    }

                    return Name.Equals(cn.Name);
                }
                catch (Exception ex)
                {
                    Logger.Log.Warn(ex.Message, ex);
                    return false;
                }
            }
        }
    }
}
