using System;
using System.Threading;
using Privatix.Core.Delegates;


namespace Privatix.Core
{
    public static class DnsChangerRunner
    {
        static DnsChangerRunner()
        {
            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
        }

        public static bool Init()
        {
            return true;
        }

        static void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    DnsChanger.Instance.DisableDnsLeakGuard(o);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            }
            );
        }

        static void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            if (!SyncConnector.Instance.ChangingServer)
            {
                if (Config.DnsLeak)
                {
                    ThreadPool.QueueUserWorkItem(o =>
                        {
                            try
                            {
                                DnsChanger.Instance.EnableDnsLeakGuard(o);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error(ex.Message, ex);
                            }
                        }
                        );
                }
            }
        }
    }
}
