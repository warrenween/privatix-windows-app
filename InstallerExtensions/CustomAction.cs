using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using Privatix.Core;


namespace InstallerExtensions
{
    public static class CustomActions
    {
        [CustomAction]
        public static ActionResult ReportInstalled(Session session)
        {
            try
            {
                Task eventTask = new Task(() => GoogleAnalyticsApi.TrackEventAndWait("install", "win"));
                eventTask.Start();
                eventTask.Wait(10000);
            }
            catch
            {
                
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult ReportUninstalled(Session session)
        {
            string deviceId = "";
            string subscriptionId = "";

            try
            {
                Config.Load();
                subscriptionId = Config.Subscription_Id;
                deviceId = Config.GetDeviceId();
            }
            catch
            {
            }

            try
            {
                string link = string.Format("https://privatix.com/uninstall/?utm_nooverride=1&device={0}&subscription={1}", deviceId, subscriptionId);
                Process.Start(link);
            }
            catch
            {
            }

            try
            {
                Task eventTask = new Task(() => GoogleAnalyticsApi.TrackEventAndWait("uninstall", "win"));
                eventTask.Start();
                eventTask.Wait(5000);
            }
            catch
            {

            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult RemoveConnection(Session session)
        {
            try
            {
                RasConnector.DoAnyDisconnect();
                RasConnector.RemoveEntry();
            }
            catch
            {

            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult ClosePrograms(Session session)
        {
            try
            {
                bool tryCreateNewApp;
                EventWaitHandle closeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Config.CloseEventName, out tryCreateNewApp);
                if (!tryCreateNewApp)
                {
                    closeEvent.Set();
                }
                else
                {
                    Process[] processesList = Process.GetProcesses();
                    foreach (Process process in processesList)
                    {
                        try
                        {
                            if (process.ProcessName.Equals("privatix.exe", StringComparison.CurrentCultureIgnoreCase))
                            {
                                process.Kill();
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                closeEvent.Close();
            }
            catch
            {

            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult RemoveBundle(Session session)
        {
            try
            {
                RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                RegistryKey key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);
                string[] subkeyNames = key.GetSubKeyNames();
                foreach (string name in subkeyNames)
                {
                    RegistryKey subKey = key.OpenSubKey(name, true);
                    string[] value = (string[])subKey.GetValue("BundleUpgradeCode", null);
                    if (value != null && value[0] != null)
                    {
                        if (value[0] == "{68A84909-E65E-4611-9B3A-148F54E4FF59}")
                        {
                            string filename = (string)subKey.GetValue("BundleCachePath", "");
                            if (!string.IsNullOrEmpty(filename))
                            {
                                try
                                {
                                    File.Delete(filename);
                                }
                                catch
                                {

                                }
                            }

                            subKey.Close();

                            key.DeleteSubKeyTree(name, false);
                        }
                    }
                }

                key.Close();
                hklm.Close();
            }
            catch
            {

            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult SaveLogsDir(Session session)
        {
            try
            {
                Registry.SetValue(Config.RegRootMachine, "LogsDir", Config.DefaultLogDir, RegistryValueKind.String);
            }
            catch
            {

            }

            return ActionResult.Success;
        }
    }
}
