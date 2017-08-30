using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Privatix.Core.Delegates;
using Privatix.Core.OpenVpn;


namespace Privatix.Core
{
    public class SyncConnector
    {
        private Reconnector reconnector = null;
        private bool goodConnection = false;
        private bool manualDisconnect = true;
        private bool changingServer = false;
        private bool nodeChangeDisconnect = false;
        private bool isConnected = false;
        private bool beginConnect = false;
        private bool beginDisconnect = false;
        private InternalServerEntry currentEntry = null;
        private VpnProtocol currentProtocol = VpnProtocol.IPsec;
        private InternalServerEntry changeServerEntry = null;
        private object syncConnect = new object();


        private SyncConnector()
        {
            GlobalEvents.OnConnecting += GlobalEvents_OnConnecting;
            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
            GlobalEvents.OnEnded += GlobalEvents_OnEnded;
        }


        private static SyncConnector _instance = null;
        public static SyncConnector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SyncConnector();
                }

                return _instance;
            }
        }


        public bool ManualDisconnect
        {
            get
            {
                return manualDisconnect;
            }
        }

        public bool ChangingServer
        {
            get
            {
                return changingServer;
            }
        }

        public bool NodeChangeDisconnect
        {
            get
            {
                return nodeChangeDisconnect;
            }
        }

        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
        }

        public InternalServerEntry CurrentEntry
        {
            get
            {
                return currentEntry;
            }
        }

        public VpnProtocol CurrentProtocol
        {
            get { return currentProtocol; }
        }

        public InternalStatus Status
        {
            get
            {
                return currentEntry == null ? InternalStatus.Disconnected : currentEntry.ConnectStatus;
            }
        }


        public void BeginConnect(InternalServerEntry entry)
        {
            lock (syncConnect)
            {
                if (beginConnect)
                {
                    Logger.Log.Error("Already start connecting...");
                    return;
                }

                beginConnect = true;
                currentEntry = entry;
                manualDisconnect = false;
                nodeChangeDisconnect = false;
                changingServer = false;

                ThreadPool.QueueUserWorkItem(entry.DoConnect, 0);
            }
        }

        public void BeginConnect(InternalServerEntry entry, int index)
        {
            lock (syncConnect)
            {
                if (beginConnect)
                {
                    Logger.Log.Error("Already start connecting...");
                    return;
                }

                beginConnect = true;
                currentEntry = entry;
                manualDisconnect = false;
                nodeChangeDisconnect = false;
                changingServer = false;

                ThreadPool.QueueUserWorkItem(entry.DoConnect, index);
            }
        }

        public void BeginDisconnect(InternalServerEntry entry, bool manual = false, bool nodeChange = false)
        {
            lock (syncConnect)
            {
                //Logger.Log.Info(Environment.StackTrace);

                if (beginDisconnect)
                {
                    Logger.Log.Error("Already start disconnecting...");
                    return;
                }
                if (entry != currentEntry)
                {
                    Logger.Log.ErrorFormat("Disconnect entry != current entry {0} {1}", currentEntry != null ? currentEntry.Country : "(null)", entry.Country);
                }

                beginDisconnect = true;
                manualDisconnect = manual;
                nodeChangeDisconnect = nodeChange;
                ThreadPool.QueueUserWorkItem(entry.DoDisconnect);
            }
        }

        public void BeginDisconnect(bool manual = false, bool nodeChange = false)
        {
            lock (syncConnect)
            {
                //Logger.Log.Info(Environment.StackTrace);

                if (beginDisconnect)
                {
                    Logger.Log.Error("Already start disconnecting...");
                    return;
                }

                beginDisconnect = true;
                manualDisconnect = manual;
                nodeChangeDisconnect = nodeChange;
                if (currentEntry != null && currentEntry.ConnectStatus != InternalStatus.Connecting)
                {
                    ThreadPool.QueueUserWorkItem(currentEntry.DoDisconnect);
                }
                else
                {
                    DoAnyDisconnect();
                }
            }
        }

        public void Disconnect(bool manual = false, bool nodeChange = false)
        {
            lock (syncConnect)
            {
                //Logger.Log.Info(Environment.StackTrace);

                if (beginDisconnect)
                {
                    Logger.Log.Error("Already start disconnecting...");
                    return;
                }

                beginDisconnect = true;
                manualDisconnect = manual;
                nodeChangeDisconnect = nodeChange;
                if (currentEntry != null)
                {
                    currentEntry.DoDisconnect(null);
                }
            }
        }

        public void BeginChangeServer(InternalServerEntry oldEntry, InternalServerEntry newEntry)
        {
            lock (syncConnect)
            {
                //Logger.Log.Info(Environment.StackTrace);

                if (oldEntry != currentEntry)
                {
                    Logger.Log.ErrorFormat("Disconnect entry != current entry {0} {1}", currentEntry != null ? currentEntry.Country : "(null)", oldEntry.Country);
                }

                if (beginDisconnect)
                {
                    Logger.Log.Error("Already start disconnecting...");
                    return;
                }

                beginDisconnect = true;
                manualDisconnect = false;
                nodeChangeDisconnect = false;
                changingServer = true;
                changeServerEntry = newEntry;

                ThreadPool.QueueUserWorkItem(o =>
                {
                    oldEntry.DoDisconnect(null);
                }
                );
            }
        }

        public bool DoAnyDisconnect()
        {
            lock (syncConnect)
            {
                bool result = RasConnector.DoAnyDisconnect();
                result |= OpenVpnConnector.DoAnyDisconnect();
                if (currentEntry != null)
                {
                    GlobalEvents.Disconnected(this, currentEntry);
                }
                beginDisconnect = false;
                beginConnect = false;

                return result;
            }
        }

        public bool TryReconnect(InternalServerEntry entry)
        {
            //Logger.Log.Info(Environment.StackTrace);

            if (goodConnection || reconnector != null)
            {
                if (reconnector == null)
                {
                    reconnector = new Reconnector(entry);
                }
                beginConnect = false;
                return reconnector.TryConnect();
            }
            else
            {
                if (TryChangeProto())
                {
                    beginConnect = false;
                    BeginConnect(entry);
                    return true;
                }
                else
                {
                    Logger.Log.Error("All protocols failed.");
                    return false;
                }
            }
        }

        public bool TryChangeProto()
        {
            if (currentProtocol == VpnProtocol.IPsec)
            {
                currentProtocol = VpnProtocol.OpenVpnUDP;
                Logger.Log.Info("Change proto to OpenVPN UDP");
                return true;
            }
            else if (currentProtocol == VpnProtocol.OpenVpnUDP)
            {
                currentProtocol = VpnProtocol.OpenVpnTCP;
                Logger.Log.Info("Change proto to OpenVPN TCP");
                return true;
            }

            currentProtocol = VpnProtocol.IPsec;
            return false;
        }

        public void ReplaceConnectedEntry(InternalServerEntry newEntry)
        {
            if (currentEntry == null)
            {
                Logger.Log.Error("currentEntry == null");
                return;
            }

            if (currentEntry.ConnectStatus != InternalStatus.Connected)
            {
                Logger.Log.Error("currentEntry not connected " + currentEntry.ConnectStatus.ToString());
                return;
            }

            currentEntry = newEntry;
        }

        private void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            //Logger.Log.Info(Environment.StackTrace);

            if (args.Entry == currentEntry)
            {
                isConnected = false;
                beginConnect = false;
                currentEntry = null;
            }

            beginDisconnect = false;

            args.Entry.CloseConnector();
        }

        private void GlobalEvents_OnEnded(object sender, ServerEntryEventArgs args)
        {
            lock (syncConnect)
            {
                if (changingServer && changeServerEntry != null)
                {
                    changingServer = false;
                    currentEntry = changeServerEntry;
                    beginConnect = true;
                    manualDisconnect = false;
                    nodeChangeDisconnect = false;                    

                    ThreadPool.QueueUserWorkItem(changeServerEntry.DoConnect, 0);
                    changingServer = false;
                    changeServerEntry = null;
                }
            }
        }

        private void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            reconnector = null;
            goodConnection = true;
            isConnected = true;
            if (currentEntry == null)
            {
                currentEntry = args.Entry;
            }
        }

        private void GlobalEvents_OnConnecting(object sender, ServerEntryEventArgs args)
        {
            goodConnection = false;
        }

        public void ChangeCurrentProtocol(VpnProtocol newProtocol)
        {
            if (isConnected)
            {
                return;
            }

            currentProtocol = newProtocol;
        }
    }
}
