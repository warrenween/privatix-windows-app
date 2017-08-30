using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;


namespace Privatix
{
    public static class Updater
    {
        private static PrivatixWebClient webClient;
        private static DownloadProgress downloadWindow;
        private static Timer timer;
        private static bool allowUpdates = true;


        static Updater()
        {
            GlobalEvents.OnConnecting += (sender, args) => { allowUpdates = false; };
            GlobalEvents.OnConnected += (sender, args) => { allowUpdates = true; };
            GlobalEvents.OnDisconnecting += (sender, args) => { allowUpdates = false; };
            GlobalEvents.OnDisconnected += (sender, args) => { allowUpdates = true; };
            GlobalEvents.OnForceUpdate += GlobalEvents_OnForceUpdate;
        }

        static void GlobalEvents_OnForceUpdate(object sender, EventArgs e)
        {
            if (!App.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.Invoke(new EventHandler(GlobalEvents_OnForceUpdate),
                    DispatcherPriority.Normal,
                    new object[] {sender, e});
                return;
            }

            if (MessageBox.Show("Your version is deprecated. You must update Privatix to continue. Do you want update?", "Privatix", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                Logger.Log.Info("User do not want force update");
                Application.Current.Shutdown();
                return;
            }

            DoUpdate();
        }


        private static void ClearUpdates()
        {
            try
            {
                var file = Path.Combine(Config.UpdatePath, "update.bat");
                if (File.Exists(file))
                    File.Delete(file);
                file = Path.Combine(Config.UpdatePath, "PrivatixSetup.msi");
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Unable to delete update-backup-file.", ex);
            }
        }

        private static void Update()
        {
            if (!App.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.Invoke(new SimpleHandler(Update), DispatcherPriority.Normal);
                return;
            }
            
            if (MessageBox.Show("There is an update available, do you want to install it?", "Privatix Update Available", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                Logger.Log.Info("User do not want update");
                return;
            }

            DoUpdate();
        }

        private static void DoUpdate()
        {
            try
            {
                if (!Directory.Exists(Config.UpdatePath))
                    Directory.CreateDirectory(Config.UpdatePath);

                string updateBat = string.Format("msiexec /x {0} IS_UPDATE=1 /passive /l*v \"{1}\"\n" +
                                                "{2} IS_UPDATE=1 /passive /l*v \"{3}\"\n" +
                                                "del PrivatixSetup.msi\n" +
                                                "del update.bat\n",
                        "{7AA3DFFA-BC1A-4484-88B6-6A85EF5D8B33}",
                        Path.Combine(Config.LogPath, "RemoveLog.txt"),
                        Path.Combine(Config.UpdatePath, "PrivatixSetup.msi"),
                        Path.Combine(Config.LogPath, "InstallLog.txt"));

                File.WriteAllText(Path.Combine(Config.UpdatePath, "update.bat"), updateBat);

                downloadWindow = new DownloadProgress();
                Logger.Log.Info("Download updates file: " + Config.UpdateBaseUri + "/PrivatixSetup.msi");
                webClient = new PrivatixWebClient();
                webClient.DownloadProgressChanged += DownloadProgressChanged;
                webClient.DownloadFileCompleted += DownloadFileCompleted;
                webClient.DownloadFileAsync(new Uri(Config.UpdateBaseUri + "/PrivatixSetup.msi"), Path.Combine(Config.UpdatePath, "PrivatixSetup.msi"));

                downloadWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Unable to update: " + ex.Message, ex);
            }
        }

        private static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (downloadWindow != null)
            {
                downloadWindow.ChangeProgress((double)e.ProgressPercentage);
            }
        }

        private static void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                Process p = new Process();
                p.StartInfo.FileName = Path.Combine(Config.UpdatePath, "update.bat");
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                Logger.Log.Info("Start Update");
                p.Start();
                 
                Updater.StartRestart();
            }
            else
            {
                if (downloadWindow != null)
                {
                    downloadWindow.Close();
                }
            }
        }

        public static void CancelDownload()
        {
            if (webClient != null)
            {
                webClient.CancelAsync();
            }
        }

        public static void Start()
        {
            ClearUpdates();


            CheckForUpdates();


            if (Config.UpdateDelta != 0)
            {
                timer = new Timer(DoMonitoring);
                timer.Change(Config.UpdateDelta * 1000, Timeout.Infinite);
            }
        }

        public static void Stop()
        {
            if (timer != null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private static void DoMonitoring(object obj)
        {
            try
            {
                CheckForUpdates();
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Check for updates error", ex);
            }
            finally
            {
                timer.Change(Config.UpdateDelta * 1000, Timeout.Infinite);
            }
        }

        private static void CheckForUpdates()
        {
            if (Config.AutoUpdate)
            {
                if (!allowUpdates)
                    return;

                if (!IsNeedUpdate())
                    return;

                Update();
            }
        }

        private static bool IsNeedUpdate()
        {
            try
            {
                Version newVersion = new Version("0.0.0.0");
                Version thisVersion = new Version("0.0.0.0");
                webClient = new PrivatixWebClient();
                Logger.Log.Info("Check server version " + Config.UpdateBaseUri + "/version.txt");
                newVersion = Version.Parse(webClient.DownloadString(Config.UpdateBaseUri + "/version.txt"));
                thisVersion = Version.Parse(Config.Version);
                Logger.Log.Info("Server: " + newVersion.ToString() + "  Client: " + thisVersion.ToString());
                int need = thisVersion.CompareTo(newVersion);
                if (need >= 0)
                {
                    //Logger.Log.Info(newVersion.ToString() + " <= " + thisVersion.ToString());
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Check version error", ex);

                if (timer == null)
                {
                    timer = new Timer(DoMonitoring);
                }
                timer.Change(5 * 60 * 1000, Timeout.Infinite);

                return false;
            }
        }

        private static void StartRestart()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.Invoke(new Action(StartRestart));
                return;
            }

            App.Tray.tiTaskbarIcon.Visibility = Visibility.Collapsed;

            Environment.Exit(1);
        }
    }
}
