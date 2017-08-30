using System;
using System.Linq;
using System.Threading;
using Microsoft.Win32;


namespace Privatix.Core
{
    public class TimeZoneChanger : MarshalByRefObject
    {
        private object lockObj = new object();
        private string savedTimeZoneId = "";
        private string currentTimeZoneId = "";

        private static TimeZoneChanger _instance = null;
        public static TimeZoneChanger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TimeZoneChanger();
                }

                return _instance;
            }
        }


        public TimeZoneChanger()
        {
            TokenPrivilegesAccess.EnablePrivilege("SeTimeZonePrivilege");

            currentTimeZoneId = TimeZoneInfo.Local.Id;
            LoadOldTimeZone(out savedTimeZoneId);
        }

        private void SaveOldTimeZone(string oldTimeZoneId)
        {
            string oldTimeZone = (string)Registry.GetValue(Config.RegRootMachine, "Timezone", "");
            if (!string.IsNullOrEmpty(oldTimeZone))
            {
                Logger.Log.Info("Timezone already saved");
                return;
            }

            Logger.Log.Info("oldTimeZoneId = " + oldTimeZoneId);

            Registry.SetValue(Config.RegRootMachine, "Timezone", oldTimeZoneId);
            savedTimeZoneId = oldTimeZoneId;
        }

        private bool LoadOldTimeZone(out string oldTimeZone)
        {
            oldTimeZone = (string)Registry.GetValue(Config.RegRootMachine, "Timezone", "");

            if (oldTimeZone != null)
            {
                Logger.Log.Info("oldTimeZoneId = " + oldTimeZone);
            }
            else
            {
                Logger.Log.Info("oldTimeZoneId = null");
            }            

            return !string.IsNullOrEmpty(oldTimeZone);
        }

        private void RemoveOldTimeZone()
        {
            Registry.SetValue(Config.RegRootMachine, "Timezone", "");
            savedTimeZoneId = "";
        }

        public void ChangeTimeZone(TimeSpan timeZoneSpan)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    lock (lockObj)
                    {
                        Logger.Log.Info("timeSpan = " + timeZoneSpan.ToString());

                        var timeZones = TimeZoneInfo.GetSystemTimeZones();

                        var currentTimeZone = timeZones.FirstOrDefault(t => t.Id == currentTimeZoneId);
                        if (currentTimeZone != null)
                        {
                            Logger.Log.InfoFormat("Current timezone: {0}, offset: {1}", currentTimeZoneId, currentTimeZone.BaseUtcOffset);
                            if (currentTimeZone.BaseUtcOffset == timeZoneSpan)
                            {
                                Logger.Log.Info("timeSpan == currentTimeZone.BaseUtcOffset");

                                return;
                            }
                        }

                        var newTimeZone = timeZones.FirstOrDefault(t => t.BaseUtcOffset == timeZoneSpan);
                        if (newTimeZone == null)
                        {
                            Logger.Log.ErrorFormat("Time zone not found for offset {0}", timeZoneSpan.ToString());
                            return;
                        }

                        if (TimeZoneHelper.SetTimeZone(newTimeZone.Id))
                        {
                            SaveOldTimeZone(currentTimeZoneId);
                            currentTimeZoneId = newTimeZone.Id;
                        }                    
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            }
            );
        }

        public void RestoreTimeZone()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    lock (lockObj)
                    {
                    
                        if (string.IsNullOrEmpty(savedTimeZoneId))
                        {
                            Logger.Log.Info("No saved TimeZone");
                            return;
                        }

                        Logger.Log.Info("savedTimeZoneId = " + savedTimeZoneId);

                        if (TimeZoneHelper.SetTimeZone(savedTimeZoneId))
                        {
                            currentTimeZoneId = savedTimeZoneId;
                            RemoveOldTimeZone();                            
                        }                    
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            }
            );
        }

        public override object InitializeLifetimeService()
        {
            return (null);
        }
    }
}
