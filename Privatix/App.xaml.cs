using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Bugsnag.Clients;
using Privatix.Controls;
using Privatix.Core;
using Privatix.Core.Delegates;
using Privatix.Core.Notification;
using Privatix.Core.OpenVpn;


namespace Privatix
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static TrayWindow Tray = null;
        public static SiteConnector Site = null;
        public static SyncConnector Connector = null;
        private static EventWaitHandle globalEvent;
        private static EventWaitHandle closeEvent;
        public static EventWaitHandle ReconnectEvent = null;
        private static GuardRunner guardRunner = null;
        public static bool Connected = false;
        private static NetworkWatcher networkWatcher = null;
        private static DateTime lastUserAction = DateTime.Now;
        public static SpeedTester SpeedTester = null;
        private static bool isUser = false; //if we have user session on startup


        protected override void OnStartup(StartupEventArgs e)
        {
            bool tryCreateNewApp;
            globalEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Config.GlobalEventName, out tryCreateNewApp);
            if (!tryCreateNewApp)
            {
                try
                {
                    globalEvent.Set();
                }
                catch (Exception) { }
                Environment.Exit(0);
            }

            closeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Config.CloseEventName, out tryCreateNewApp);

            base.OnStartup(e);

            //RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            //Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 50 });

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Logger.InitLogger();
            Config.Load();
#if _DEV_API_
            Logger.Log.Info("Privatix app started, version: " + Config.Version + " dev");
#elif _STAGE_API_
            Logger.Log.Info("Privatix app started, version: " + Config.Version + " stage");
#else
            Logger.Log.Info("Privatix app started, version: " + Config.Version);
#endif
            Logger.Log.Info("Device name: " + Config.GetDeviceName());
            Logger.Log.Info("Windows version: " + Config.GetFullWindowsVersion());

            WPFClient.Config.AppVersion = Config.Version;
#if _DEV_API_
            WPFClient.Config.ReleaseStage = "development";
#elif _STAGE_API_
            WPFClient.Config.ReleaseStage = "staging";
#else
            WPFClient.Config.ReleaseStage = "production";
#endif
            WPFClient.Start();


            if (!CheckIsWindowsVersionAvailable())
            {
                MessageBox.Show("You Windows version not supported. Please update Windows.", "Error", MessageBoxButton.OK);
                Environment.Exit(-4);
            }

            if (!CheckService())
            {
                MessageBox.Show("Privatix service not found. Please reinstall app.", "Error", MessageBoxButton.OK);
                Environment.Exit(-3);
            }

            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
            Hashtable channelConfig = new Hashtable();
            channelConfig["name"] = "ipc";
            channelConfig["portName"] = "PrivatixClientIpcChannel";
            SecurityIdentifier id = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            channelConfig["authorizedGroup"] = id.Translate(typeof(NTAccount)).Value;
            IpcChannel clientChannel = new IpcChannel(channelConfig, clientProvider, serverProvider);
            ChannelServices.RegisterChannel(clientChannel, false);
            RemotingConfiguration.RegisterWellKnownClientType(typeof(Guard), "ipc://PrivatixIpcChannel/Guard.rem");
            RemotingConfiguration.RegisterWellKnownClientType(typeof(DnsChanger), "ipc://PrivatixIpcChannel/DnsChanger.rem");
            RemotingConfiguration.RegisterWellKnownClientType(typeof(TimeZoneChanger), "ipc://PrivatixIpcChannel/TimeZoneChanger.rem");
            RemotingConfiguration.RegisterWellKnownClientType(typeof(ServiceWorker), "ipc://PrivatixIpcChannel/ServiceWorker.rem");
            RemotingConfiguration.RegisterWellKnownClientType(typeof(OpenVpnHelper), "ipc://PrivatixIpcChannel/OpenVpnHelper.rem");

            Connector = SyncConnector.Instance;
            Site = SiteConnector.Instance;            

            Tray = new TrayWindow();
          
            Thread th = new Thread(WaitAnotherRun);
            th.IsBackground = true;
            th.Start();

            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
            GlobalEvents.OnSecureNetworkNotify += GlobalEvents_OnSecureNetworkNotify;
            GlobalEvents.OnShowTrayNotify += GlobalEvents_OnShowTrayNotify;
            GlobalEvents.OnShowMessage += GlobalEvents_OnShowMessage;

            Tray.ShowStartupWindow();

            Task<bool> task = new Task<bool>(LoadApp);
            task.Start();
            var UISyncContext = TaskScheduler.FromCurrentSynchronizationContext();
            task.ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        StartWindow startWindow = Application.Current.MainWindow as StartWindow;
                        if (isUser)
                        {
                            startWindow.OnGetProtectedClick(null, null);
                        }
                        else
                        {
                            startWindow.ShowControls();
                        }                        
                    }
                    else
                    {
                        MessageBox.Show("Could not connect to site. Test your internet connection and try again.", "Error", MessageBoxButton.OK);
                        Application.Current.Shutdown(1);
                    }
                }, UISyncContext);
        }

        void GlobalEvents_OnShowMessage(object sender, MessageEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new MessageEventHandler(GlobalEvents_OnShowMessage),
                                  DispatcherPriority.Normal,
                                  new object[] { sender, e });
                return;
            }

            MessageBox.Show(e.Text, e.Caption, MessageBoxButton.OK);
        }


        void GlobalEvents_OnShowTrayNotify(object sender, NotificationEventArgs args)
        {
            BannerNotification notify = args.Notify as BannerNotification;
            if (notify == null)
            {
                //If null try close tray notify
                CloseTrayNotify();
                return;
            }

            if (!Site.CheckNotifyTarget(notify.Target))
            {
                return;
            }

            if (string.IsNullOrEmpty(notify.Text))
            {
                return;
            }

            if (IsUserActive())
            {
                return;
            }

            ShowTrayNotify(notify.Text, notify.Link, notify.Ttl);
        }

        private delegate void ShowTrayNotifyDelegate(string text, string link, int ttl);
        public void ShowTrayNotify(string text, string link, int ttl)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new ShowTrayNotifyDelegate(ShowTrayNotify),
                                  DispatcherPriority.Normal,
                                  new object[] { text, link, ttl });
                return;
            }

            try
            {
                MessageBalloon balloon = new MessageBalloon { tbText = { Text = text }, Link = link };
                Tray.tiTaskbarIcon.ShowCustomBalloon(balloon, PopupAnimation.Scroll, (ttl < 0) ? (int?)null : ttl * 1000);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        public void CloseTrayNotify()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new SimpleHandler(CloseTrayNotify), DispatcherPriority.Normal);
                return;
            }

            Tray.tiTaskbarIcon.CloseBalloon();
        }

        void GlobalEvents_OnSecureNetworkNotify(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new EventHandler(GlobalEvents_OnSecureNetworkNotify),
                                  DispatcherPriority.Normal,
                                  new object[] { sender, e });
                return;
            }

            if (Config.NetworkAlert)
            {
                ShowTrayNotify("Your data is at risk - turn on Privatix to protect your network activity", "privatix://protection", 10);
            }            
        }

        private void SetStartWindowsStatusText(string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new VoidStringHandler(SetStartWindowsStatusText),
                                  DispatcherPriority.Normal,
                                  new object[] { text });
                return;
            }

            StartWindow startWindow = Application.Current.MainWindow as StartWindow;
            if (startWindow == null)
            {
                return;
            }

            startWindow.tbStatus.Text = text;
        }

        private void ShowReconnect()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new SimpleHandler(ShowReconnect), DispatcherPriority.Normal);
                return;
            }

            StartWindow startWindow = Application.Current.MainWindow as StartWindow;
            if (startWindow == null)
            {
                return;
            }

            startWindow.ShowReconnect();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Logger.Log.Error(ex.Message, ex);
            WPFClient.Notify(ex);
            Shutdown(1);
        }

        void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new ServerEntryEventHandler(GlobalEvents_OnDisconnected),
                                  DispatcherPriority.Normal, new object[] { sender, args });
                return;
            }

            Tray.tiTaskbarIcon.IconSource = (BitmapImage)FindResource("GrayIcon");

            if (Connected)
            {
                Connected = false;

                GoogleAnalyticsApi.TrackEvent("switch_off", "win");
                Tray.tiTaskbarIcon.ShowBalloonTip("Disconnected", " ", BalloonIcon.Info);

                if (!SyncConnector.Instance.ManualDisconnect && !SyncConnector.Instance.NodeChangeDisconnect)
                {
                    Site.SendErrorTrace(true, args.Entry.LastError, args.Entry.LastConnectionLog, args.Entry.CurrentHost.Domen);
                }
            }
            else
            {
                try
                {
                    string label = string.Format("{0}_{1}_{2}", Site.OriginalCountry.ToUpper(), args.Entry.CountryCode.ToUpper(), args.Entry.CurrentHost.Domen.Split(new char[] { '.' })[0]);
                    GoogleAnalyticsApi.TrackEvent(SyncConnector.Instance.CurrentProtocol == VpnProtocol.IPsec ? "conn_failed" : "conn_failed_openvpn", "win", label);
                    if (args.Entry != null)
                    {
                        Site.SendErrorTrace(false, args.Entry.LastError, args.Entry.LastConnectionLog, args.Entry.CurrentHost.Domen);
                    }                    
                }
                catch { }
            }
        }

        void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new ServerEntryEventHandler(GlobalEvents_OnConnected),
                                  DispatcherPriority.Normal, new object[] { sender, args });
                return;
            }

            if (!Connected)
            {
                //ThreadPool.QueueUserWorkItem(o =>
                //{
                //    Site.PostMetrics(MetricsType.Enable_proxy);
                //}
                //);
                GoogleAnalyticsApi.TrackEvent("switch_on", "win");

                
            }
            
            Connected = true;

            Tray.tiTaskbarIcon.IconSource = (BitmapImage)FindResource("BlueIcon");
            Tray.tiTaskbarIcon.ShowBalloonTip("Connected", " ", BalloonIcon.Info);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (Guard.Instance.IsGuardEnabled)
                {
                    Guard.Instance.DisableGuard();
                }

                SyncConnector.Instance.DoAnyDisconnect();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            base.OnExit(e);
        }

        private bool LoadApp()
        {
            SyncConnector.Instance.DoAnyDisconnect();

            if (Guard.Instance.IsGuardEnabled)
            {
                Guard.Instance.DisableGuard();
            }

            if (Config.AutoUpdate)
            {
                SetStartWindowsStatusText("Check for updates...");
                Updater.Start();
            }

            guardRunner = new GuardRunner();
            DnsChangerRunner.Init();
            TimeZoneChangerRunner.Init();
            networkWatcher = new NetworkWatcher();
            networkWatcher.Init();
            

            //Site.PostMetrics(MetricsType.Open_app);
            GoogleAnalyticsApi.TrackEvent("open", "win");

            CheckCompetitors();

            bool result = false;

            do
            {
                SetStartWindowsStatusText("Connecting...");

                if (!string.IsNullOrEmpty(Config.Sid))
                {
                    result = Site.GetSession();
                    isUser = true;
                }
                else
                {
                    result = Site.PostActivation();
                }

                if (!result)
                {
                    SetStartWindowsStatusText("Unable to connect\nPlease check your network and try again");
                    if (ReconnectEvent == null)
                    {
                        ReconnectEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
                    }
                    
                    ShowReconnect();
                    ReconnectEvent.WaitOne();
                }

            } while (!result);

            Site.GetNotifications();
            Site.BeginUpdateSession();

            if (!string.IsNullOrEmpty(Site.OriginalCountry))
            {
                SpeedTester = new SpeedTester(Site.OriginalCountry);
            }

            return result;
        }

        private void WaitAnotherRun()
        {
            try
            {
                WaitHandle[] handles = new WaitHandle[] { globalEvent, closeEvent };

                while (true)
                {
                    int index = WaitHandle.WaitAny(handles);
                    if (index == 0)
                    {
                        Tray.ShowActiveWindow();
                    }
                    else if (index == 1)
                    {
                        Logger.Log.Info("Close event");

                        SyncConnector.Instance.Disconnect(true);
                        Guard.Instance.OnDisconnected();
                        DnsChanger.Instance.DisableDnsLeakGuard(null);

                        Application.Current.Shutdown(0);
                    }
                }
            }
            catch { }
        }

        private bool CheckService()
        {
            try
            {
                var services = ServiceController.GetServices();
                ServiceController privatixService = services.FirstOrDefault(s => s.ServiceName == "PrivatixService");
                if (privatixService == null)
                {
                    return false;
                }

                if (privatixService.Status != ServiceControllerStatus.Running)
                {
                    privatixService.Start();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);

                return false;
            }
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Log.Error(e.Exception.Message, e.Exception);
            WPFClient.Notify(e.Exception);

            if (e.Exception.InnerException != null)
            {
                Logger.Log.Error(e.Exception.InnerException.Message, e.Exception.InnerException);
            }

            Shutdown(1);

            e.Handled = true;
        }

        public static void RegisterUserAction()
        {
            lastUserAction = DateTime.Now;
        }

        public static bool IsUserActive()
        {
            return (DateTime.Now - lastUserAction).TotalSeconds < 60;
        }

        private delegate void ShowMainWindowPageDelegate(MainWindowPage page);
        public static void ShowMainWindowPage(MainWindowPage page)
        {
            if (!Current.Dispatcher.CheckAccess())
            {
                Current.Dispatcher.Invoke(new ShowMainWindowPageDelegate(ShowMainWindowPage),
                                  DispatcherPriority.Normal, new object[] { page });
                return;
            }

            MainWindow mainWindow = Privatix.MainWindow.Instance;
            if (Application.Current.MainWindow != mainWindow)
            {
                if (Application.Current.MainWindow != null
                    && Application.Current.MainWindow is LockWindow)
                {
                    return;
                }

                mainWindow.Left = Application.Current.MainWindow.Left;
                mainWindow.Top = Application.Current.MainWindow.Top;
                Application.Current.MainWindow.Close();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }

            mainWindow.SetPage(page, true);
            Tray.ShowActiveWindow();
        }

        private void CheckCompetitors()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    var competitors = CompetitorChecker.Instance.Check(Config.Competitors);
                    if (competitors.Count<string>() > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        bool isFirst = true;

                        foreach (string comp in competitors)
                        {
                            if (!isFirst)
                            {
                                sb.Append('_');
                            }

                            sb.Append(comp);

                            isFirst = false;
                        }

                        GoogleAnalyticsApi.TrackEvent("competitor", "win", sb.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            } );
        }

        private bool CheckIsWindowsVersionAvailable()
        {
            try
            {
                if (Environment.OSVersion.Version.Major <= 5)
                {
                    return false;
                }
            }
            catch 
            {
                return true;
            }

            return true;
        }

        public static bool ShowWarningToCloseConnection()
        {
            if (MessageBox.Show("Your current secure connection will be closed. Do you want to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                return true;
            }

            return false;
        }
    }
}
