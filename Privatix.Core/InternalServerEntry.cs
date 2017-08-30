using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using Privatix.Core.OpenVpn;


namespace Privatix.Core
{
    public class InternalServerEntry
    {
        #region Types

        public class Host
        {
            public string Domen;
            public int Port;
            public TimeSpan Timezone;
        }

        #endregion


        #region Fields

        private string country;
        private string countryCode;
        private string displayName;
        private string city;
        private int priority;
        private bool isFree;
        private List<Host> hosts;
        private InternalStatus connectStatus = InternalStatus.Disconnected;
        private string login;
        private string password;
        private int currentHostIndex;
        private string lastConnectionLog;
        private string lastError;
        private IConnector connector;
        
        #endregion


        #region Properties

        public string Country
        {
            get { return country; }
        }

        public string CountryCode
        {
            get { return countryCode; }
        }

        public string DisplayName
        {
            get 
            {
                return string.IsNullOrEmpty(displayName) ? country : displayName;
            }
        }

        public string City
        {
            get { return city; }
        }

        public int Priority
        {
            get { return priority; }
        }

        public bool IsFree
        {
            get { return isFree; }
        }

        public IList<Host> Hosts
        {
            get { return hosts.AsReadOnly(); }
        }

        public InternalStatus ConnectStatus
        {
            get { return connectStatus; }
            set 
            { 
                connectStatus = value;
                //SaveConnectionState();
            }
        }

        public Host CurrentHost
        {
            get
            {
                return hosts[currentHostIndex];
            }
        }

        public int CurrentHostIndex
        {
            get
            {
                return currentHostIndex;
            }
        }

        public string CurrentDomen
        {
            get
            {
                return hosts[currentHostIndex].Domen;
            }
        }

        public int CurrentPort
        {
            get
            {
                return hosts[currentHostIndex].Port;
            }
        }

        public TimeSpan CurrentTimezone
        {
            get
            {
                return hosts[currentHostIndex].Timezone;
            }
        }

        public string LastConnectionLog
        {
            get { return lastConnectionLog; }
        }

        public string LastError
        {
            get { return lastError; }
            set { lastError = value; }
        }

        public string Login
        {
            get { return login; }
        }

        public string Password
        {
            get { return password; }
        }

        #endregion

        #region Ctors

        public InternalServerEntry(string _country, string _countryCode, string _displayName, string _city, int _priority, bool _isFree, List<Host> _hosts, string _login, string _password)
        {
            country = _country;
            countryCode = _countryCode;
            displayName = _displayName;
            city = _city;
            priority = _priority;
            isFree = _isFree;
            hosts = _hosts;
            login = _login;
            password = _password;

            currentHostIndex = 0;

            lastConnectionLog = "";
        }

        #endregion

        #region Methods

        //public void BeginConnect()
        //{
        //    Config.ManualDisconnect = false;
        //    Config.ServerChangeDisconnect = false;

        //    ThreadPool.QueueUserWorkItem(DoConnect);
        //}

        //public void BeginConnect(int index)
        //{
        //    currentHostIndex = index;
        //    Config.ManualDisconnect = false;
        //    Config.ServerChangeDisconnect = false;

        //    ThreadPool.QueueUserWorkItem(DoConnect);
        //}

        //public void BeginDisconnect()
        //{
        //    ThreadPool.QueueUserWorkItem(DoDisconnect);
        //}

        //public void BeginChangeServer(InternalServerEntry newConnect)
        //{
        //    ThreadPool.QueueUserWorkItem(o =>
        //        {
        //            DoDisconnect(null);
        //            newConnect.DoConnect(null);
        //        }
        //        );
        //}

        public void DoConnect(object obj)
        {
            if (obj != null && obj is int)
            {
                int index = (int)obj;
                if (index >= 0 && index < hosts.Count)
                {
                    currentHostIndex = index;
                }
            }

            GlobalEvents.Connecting(this, this);

            if (!SyncConnector.Instance.ChangingServer && Config.ConnectionGuard)
            {
                if (!Guard.Instance.EnableGuard())
                {
                    Logger.Log.Error("Enable Connection Guard is failed.");
                    GlobalEvents.ShowMessage(this, "Enable Connection Guard is failed. Do you want continue without Connection Guard", "Error");
                    GlobalEvents.Disconnected(this, this);
                    return;
                }
            }

            //RasConnector rasConnector = new RasConnector(this);
            //rasConnector.DoConnect();

            if (connector != null)
            {
                connector.Close();
                connector = null;
            }

            switch (SyncConnector.Instance.CurrentProtocol)
            {
                case VpnProtocol.IPsec:
                    connector = new RasConnector(this);
                    break;
                case VpnProtocol.OpenVpnUDP:
                    connector = new OpenVpnConnector(this, OpenVpnProto.UDP);
                    break;
                case VpnProtocol.OpenVpnTCP:
                    connector = new OpenVpnConnector(this, OpenVpnProto.TCP);
                    break;
                default:
                    Logger.Log.Error("Unknown protocol: " + SyncConnector.Instance.CurrentProtocol.ToString());
                return;
            }

            connector.DoConnect();
        }

        public void DoDisconnect(object obj)
        {
            if (connector != null)
            {
                connector.DoDisconnect();
            }
            else
            {
                Logger.Log.Error("DoDisconnect on entry without connector!");

                if (SyncConnector.Instance.CurrentProtocol == VpnProtocol.IPsec)
                {
                    RasConnector.DoAnyDisconnect();
                }
                else
                {
                    OpenVpnConnector.DoAnyDisconnect();
                }
            }
        }

        //private void SaveConnectionState()
        //{
        //    Registry.SetValue(Config.RegRootFull + "\\servers\\", CurrentDomen, connectStatus.ToString());
        //}

        //public void LoadConnectionState()
        //{
        //    var val = Registry.GetValue(Config.RegRootFull + "\\servers\\", hosts[0], "Disconnected") as string;
        //    ConnectStatus = val == null ? InternalStatus.Disconnected : (InternalStatus)Enum.Parse(typeof(InternalStatus), val);

        //    if (ConnectStatus != InternalStatus.Connected)
        //    {
        //        return;
        //    }

        //    var conList = RasConnection.GetActiveConnections();
        //    var conn = conList.FirstOrDefault(cn => cn.EntryName == Config.RasEntryName);
        //    if (conn == null)
        //    {
        //        ConnectStatus = InternalStatus.Disconnected;

        //    }
        //    else
        //    {
        //        SetRasEntry(conn);
        //    }
        //}

        //private void SetRasEntry(RasConnection conn)
        //{
        //    RasConnector rasConnector = new RasConnector(this);
        //    rasConnector.Handle = conn.Handle;

        //    GlobalEvents.ChangeServer(this, this);
        //}

        public bool EqualHosts(IList<Privatix.Core.Site.Host> subsriptionsHosts)
        {
            foreach (var subsriptionHost in subsriptionsHosts)
            {
                bool isFind = false;
                foreach (var host in hosts)
                {
                    if (string.Equals(subsriptionHost.host, host.Domen, StringComparison.InvariantCultureIgnoreCase)
                        && subsriptionHost.port == host.Port
                        && subsriptionHost.timezone == host.Timezone.TotalHours.ToString())
                    {
                        isFind = true;
                        break;
                    }
                }

                if (!isFind)
                {
                    return false;
                }
            }

            foreach (var host in hosts)
            {
                bool isFind = false;
                foreach (var subsriptionHost in subsriptionsHosts)
                {
                    if (string.Equals(subsriptionHost.host, host.Domen, StringComparison.InvariantCultureIgnoreCase)
                        && subsriptionHost.port == host.Port
                        && subsriptionHost.timezone == host.Timezone.TotalHours.ToString())
                    {
                        isFind = true;
                        break;
                    }
                }

                if (!isFind)
                {
                    return false;
                }
            }

            return true;
        }

        public void Update(string _country, string _displayName, int _priority, bool _isFree, string _login, string _password)
        {
            country = _country;
            displayName = _displayName;
            priority = _priority;
            isFree = _isFree;
            login = _login;
            password = _password;
        }

        public void ConnectionLogClear()
        {
            lastConnectionLog = "";
            lastError = "";
        }

        public void ConnectionLogAddLine(string logLine)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(lastConnectionLog);
            sb.Append('[');
            sb.Append(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
            sb.Append("] ");
            sb.Append(logLine);
            sb.Append('\n');
            lastConnectionLog = sb.ToString();
        }

        public void CloneEntryStatus(InternalServerEntry oldEntry)
        {
            connectStatus = oldEntry.ConnectStatus;
            lastConnectionLog = oldEntry.LastConnectionLog;
            currentHostIndex = 0;
            connector = oldEntry.connector;
            for (int i = 0; i < hosts.Count; i++)
            {
                if (hosts[i].Domen.Equals(oldEntry.CurrentDomen, StringComparison.CurrentCultureIgnoreCase)
                    && hosts[i].Port == oldEntry.CurrentPort)
                {
                    currentHostIndex = i;
                    break;
                }
            }
        }

        public void CloseConnector()
        {
            if (connector != null)
            {
                connector.Close();
                connector = null;
            }
        }

        public void SetCurrentHostIndex(int index)
        {
            if (index >= hosts.Count)
            {
                return;
            }

            currentHostIndex = index;
        }

        #endregion
    }

    /// <summary>
    /// Внутренний статус подключения
    /// </summary>
    public enum InternalStatus
    {
        Disconnected,
        Connected,
        ReConnecting,
        Connecting,
        Disconnecting,
        Error
    }
}
