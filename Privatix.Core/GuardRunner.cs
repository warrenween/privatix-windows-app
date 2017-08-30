using System;
using System.Net.NetworkInformation;
using Privatix.Core.Delegates;


namespace Privatix.Core
{
    public class GuardRunner
    {
        private static GuardRunner _instance = null;
        public static GuardRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GuardRunner();
                }

                return _instance;
            }
        }

        public GuardRunner()
        {
            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }

        void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            try
            {
                if (!SyncConnector.Instance.ManualDisconnect)
                {
                    Guard.Instance.CheckGuardWork();
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            try
            {
                if (!SyncConnector.Instance.ManualDisconnect)
                {
                    Guard.Instance.CheckGuardWork();
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            if (!SyncConnector.Instance.ChangingServer)
            {
                GuardStatus guardStatus;

                try
                {
                    guardStatus = Guard.Instance.Status;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                    guardStatus = GuardStatus.Error;
                }

                if (SyncConnector.Instance.ManualDisconnect || guardStatus == GuardStatus.Initialized)
                {
                    Guard.Instance.OnDisconnected();
                }
            }            
        }

        void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            Guard.Instance.OnConnected();
        }
    }
}
