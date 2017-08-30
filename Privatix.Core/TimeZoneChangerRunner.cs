using System;
using System.Threading;
using Privatix.Core.Delegates;


namespace Privatix.Core
{
    public static class TimeZoneChangerRunner
    {
        static TimeZoneChangerRunner()
        {
            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
        }

        public static void Init()
        {
            TimeZoneChanger.Instance.RestoreTimeZone();
        }

        static void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            ThreadPool.QueueUserWorkItem(o =>
                {
                    try
                    {
                        TimeZoneChanger.Instance.RestoreTimeZone();
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
            if (Config.AdjustSystemTime)
            {
                ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            TimeZoneChanger.Instance.ChangeTimeZone(args.Entry.CurrentTimezone);
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
