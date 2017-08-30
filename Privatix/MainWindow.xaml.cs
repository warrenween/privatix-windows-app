using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;
using Privatix.Core.Notification;


namespace Privatix
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowPage currentPage;
        private Stack<MainWindowPage> prevPages = new Stack<MainWindowPage>();
        private static MainWindow instance = null;
        private Timer bannerTimer = null;
        private bool bannerVisible = false;
        private int devModeClicks = 0;


        #region dependency property bool DevMode
        public bool DevMode
        {
            get
            {
                return (bool)GetValue(DevModeProperty);
            }
            private set
            {
                SetValue(DevModeProperty, value);
            }
        }

        public static readonly DependencyProperty DevModeProperty =
            DependencyProperty.Register("DevMode", typeof(bool), typeof(MainWindow),
                                        new FrameworkPropertyMetadata(false));
        #endregion


        public MainWindow()
        {
            InitializeComponent();

            GlobalEvents.OnShowBanner += GlobalEvents_OnShowBanner;
            GlobalEvents.OnDisconnectTimerUpdated += GlobalEvents_OnDisconnectTimerUpdated;
            currentPage = MainWindowPage.None;
        }

        void GlobalEvents_OnDisconnectTimerUpdated(object sender, IntEventArgs args)
        {
            if (args.Param < 0)
            {
                disconnectTimer.Stop();
            }
            else
            {
                disconnectTimer.Start(args.Param);
            }
        }

        void GlobalEvents_OnShowBanner(object sender, NotificationEventArgs args)
        {
            BannerNotification notify = args.Notify as BannerNotification;
            if (notify == null)
            {
                return;
            }
            if (App.Site.CheckNotifyTarget(notify.Target))
            {
                ShowBanner(notify);
            }            
        }

        public static MainWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MainWindow();
                    instance.SetPage(MainWindowPage.Protection, false);
                }

                return instance;
            }
        }

        public void SetPage(MainWindowPage page, bool needBack)
        {
            if (page == MainWindowPage.Protection)
            {
                ctrlProtection.StartAnimation();
            }
            else
            {
                ctrlProtection.StopAnimation();
            }

            ResetDevModeClicks();

            switch (page)
            {
                case MainWindowPage.SignIn:
                {
                    ctrlIpAddress.Visibility = Visibility.Collapsed;
                    ctrlProtection.Visibility = Visibility.Collapsed;
                    ctrlSettings.Visibility = Visibility.Collapsed;
                    ctrlSignIn.Visibility = Visibility.Visible;
                    ctrlSignUp.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovery.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovered.Visibility = Visibility.Collapsed;
                    ctrlServerList.Visibility = Visibility.Collapsed;
                    break;
                }
                case MainWindowPage.SignUp:
                {
                    ctrlIpAddress.Visibility = Visibility.Collapsed;
                    ctrlProtection.Visibility = Visibility.Collapsed;
                    ctrlSettings.Visibility = Visibility.Collapsed;
                    ctrlSignIn.Visibility = Visibility.Collapsed;
                    ctrlSignUp.Visibility = Visibility.Visible;
                    ctrlPasswordRecovery.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovered.Visibility = Visibility.Collapsed;
                    ctrlServerList.Visibility = Visibility.Collapsed;
                    break;
                }
                case MainWindowPage.Recovery:
                {
                    ctrlIpAddress.Visibility = Visibility.Collapsed;
                    ctrlProtection.Visibility = Visibility.Collapsed;
                    ctrlSettings.Visibility = Visibility.Collapsed;
                    ctrlSignIn.Visibility = Visibility.Collapsed;
                    ctrlSignUp.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovery.Visibility = Visibility.Visible;
                    ctrlPasswordRecovered.Visibility = Visibility.Collapsed;
                    ctrlServerList.Visibility = Visibility.Collapsed;
                    break;
                }
                case MainWindowPage.Recovered:
                {
                    ctrlIpAddress.Visibility = Visibility.Collapsed;
                    ctrlProtection.Visibility = Visibility.Collapsed;
                    ctrlSettings.Visibility = Visibility.Collapsed;
                    ctrlSignIn.Visibility = Visibility.Collapsed;
                    ctrlSignUp.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovery.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovered.Visibility = Visibility.Visible;
                    ctrlServerList.Visibility = Visibility.Collapsed;
                    break;
                }
                case MainWindowPage.Protection:
                {
                    ctrlIpAddress.Visibility = Visibility.Collapsed;
                    ctrlProtection.Visibility = Visibility.Visible;
                    ctrlSettings.Visibility = Visibility.Collapsed;
                    ctrlSignIn.Visibility = Visibility.Collapsed;
                    ctrlSignUp.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovery.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovered.Visibility = Visibility.Collapsed;
                    ctrlServerList.Visibility = Visibility.Collapsed;
                    prevPages.Clear();

                    break;
                }
                case MainWindowPage.Settings:
                {
                    ctrlIpAddress.Visibility = Visibility.Collapsed;
                    ctrlProtection.Visibility = Visibility.Collapsed;
                    ctrlSettings.Visibility = Visibility.Visible;
                    ctrlSignIn.Visibility = Visibility.Collapsed;
                    ctrlSignUp.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovery.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovered.Visibility = Visibility.Collapsed;
                    ctrlServerList.Visibility = Visibility.Collapsed;
                    break;
                }
                case MainWindowPage.IpAddress:
                {
                    ctrlIpAddress.Visibility = Visibility.Visible;
                    ctrlProtection.Visibility = Visibility.Collapsed;
                    ctrlSettings.Visibility = Visibility.Collapsed;
                    ctrlSignIn.Visibility = Visibility.Collapsed;
                    ctrlSignUp.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovery.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovered.Visibility = Visibility.Collapsed;
                    ctrlServerList.Visibility = Visibility.Collapsed;
                    break;
                }
                case MainWindowPage.SelectList:
                {
                    ctrlIpAddress.Visibility = Visibility.Collapsed;
                    ctrlProtection.Visibility = Visibility.Collapsed;
                    ctrlSettings.Visibility = Visibility.Collapsed;
                    ctrlSignIn.Visibility = Visibility.Collapsed;
                    ctrlSignUp.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovery.Visibility = Visibility.Collapsed;
                    ctrlPasswordRecovered.Visibility = Visibility.Collapsed;
                    ctrlServerList.Visibility = Visibility.Visible;
                    ctrlServerList.FillServersList();
                    break;
                }                
            }

            App.RegisterUserAction();

            if (needBack)
            {
                if (currentPage != page)
                {
                    prevPages.Push(currentPage);
                }                
            }


            if (prevPages.Count > 0 && prevPages.Peek() != MainWindowPage.None)
            {
                btnBack.Visibility = Visibility.Visible;
            }
            else
            {
                btnBack.Visibility = Visibility.Hidden;
            }

            currentPage = page;
        }

        private void btnIp_Click(object sender, RoutedEventArgs e)
        {
            SetPage(MainWindowPage.IpAddress, true);
            GoogleAnalyticsApi.TrackEvent("ipcheck_screen", "win");
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPage(MainWindowPage.Settings, true);
            GoogleAnalyticsApi.TrackEvent("settings", "win");
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (prevPages.Count > 0)
            {
                MainWindowPage page = prevPages.Pop();
                if (page != MainWindowPage.None)
                {
                    SetPage(page, false);
                    return;
                }
            }

            SetPage(MainWindowPage.Protection, false);
        }

        public void DisableControls()
        {
            Logger.Log.Info("DisableControls");

            foreach (UIElement element in spContainer.Children)
            {
                if (element == ctrlProtection)
                    continue;
                element.IsEnabled = false;
            }
            ctrlProtection.chbSwitchProtection.IsEnabled = false;
            ctrlProtection.btnSelectServer.IsEnabled = false;

            Mouse.OverrideCursor = Cursors.AppStarting;
        }

        public void EnableControls()
        {
            Logger.Log.Info("EnableControls");
            ctrlProtection.chbSwitchProtection.IsEnabled = true;
            ctrlProtection.btnSelectServer.IsEnabled = true;
            foreach (UIElement element in spContainer.Children)
            {
                element.IsEnabled = true;
            }

            Mouse.OverrideCursor = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Minimized;

            GoogleAnalyticsApi.TrackEvent("exit", "win");
        }

        public void CloseBanner(object state)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new TimerCallback(CloseBanner),
                                  DispatcherPriority.Normal,
                                  new object[] { state });
                return;
            }

            bannerVisible = false;

            if (bannerTimer != null)
            {
                bannerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0;
            animation.To = -ctrlBanner.ActualHeight;
            animation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            ctrlBanner.BeginAnimation(Canvas.BottomProperty, animation);
        }

        public void ShowBanner(INotification notification)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new VoidINotificationHandler(ShowBanner),
                                  DispatcherPriority.Normal,
                                  new object[] { notification });
                return;
            }

            BannerNotification bannerNotification = notification as BannerNotification;
            if (bannerNotification == null)
            {
                return;
            }

            ctrlBanner.tbText.FontFamily = new FontFamily("Segoe UI Semibold");
            ctrlBanner.tbLine1.FontSize = 20;
            ctrlBanner.tbLine2.FontSize = 20;
            ctrlBanner.tbLine3.FontSize = 12;
            if (string.IsNullOrEmpty(bannerNotification.Url))
            {
                ctrlBanner.imgBanner.Visibility = Visibility.Collapsed;
                ctrlBanner.tbText.Visibility = Visibility.Visible;
                ctrlBanner.tbLine1.Text = bannerNotification.Text;
                ctrlBanner.tbLine2.Text = "";
                ctrlBanner.tbLine3.Text = "\n";
            }
            else
            {
                if (bannerNotification.BannerImage == null)
                {
                    try
                    {
                        if (bannerNotification.ImageBytes != null)
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.Default;
                            image.StreamSource = new MemoryStream(bannerNotification.ImageBytes);
                            image.EndInit();
                            bannerNotification.BannerImage = image;
                        }
                        else
                        {
                            bannerNotification.BannerImage = new BitmapImage(new Uri(bannerNotification.Url));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("Error load bitmap image", ex);
                        return;
                    }
                }
                ctrlBanner.imgBanner.Visibility = Visibility.Visible;
                ctrlBanner.tbText.Visibility = Visibility.Collapsed;
                BitmapImage bitmapImage = (BitmapImage)bannerNotification.BannerImage;
                ctrlBanner.imgBanner.Source = bitmapImage;
                ctrlBanner.imgBanner.Width = bitmapImage.Width;
                ctrlBanner.imgBanner.Height = bitmapImage.Height;
                ctrlBanner.tbLine1.Text = "";
                ctrlBanner.tbLine2.Text = "";
                ctrlBanner.tbLine3.Text = "";
            }

            ctrlBanner.Link = bannerNotification.Link;

            if (bannerTimer == null)
            {
                bannerTimer = new Timer(CloseBanner);
            }
            bannerTimer.Change(bannerNotification.Ttl * 1000, Timeout.Infinite);

            if (bannerVisible)
            {
                return;
            }
            bannerVisible = true;

            ctrlBanner.Visibility = System.Windows.Visibility.Visible;
            Canvas.SetBottom(ctrlBanner, -ctrlBanner.ActualHeight);
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = -ctrlBanner.ActualHeight;
            animation.To = 0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            ctrlBanner.BeginAnimation(Canvas.BottomProperty, animation);
        }

        public void ShowDisconnectTimerBanner()
        {
            ctrlBanner.imgBanner.Visibility = Visibility.Visible;
            ctrlBanner.tbText.Visibility = Visibility.Collapsed;
            ctrlBanner.Link = Config.DisconnectBannerPremiumUrl;
            BitmapImage bitmapImage = (BitmapImage)FindResource("TimerBanner");
            ctrlBanner.imgBanner.Source = bitmapImage;
            ctrlBanner.imgBanner.Width = bitmapImage.Width;
            ctrlBanner.imgBanner.Height = bitmapImage.Height;

            if (bannerVisible)
            {
                return;
            }
            bannerVisible = true;

            ctrlBanner.Visibility = System.Windows.Visibility.Visible;
            Canvas.SetBottom(ctrlBanner, -ctrlBanner.ActualHeight);
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = -ctrlBanner.ActualHeight;
            animation.To = 0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            ctrlBanner.BeginAnimation(Canvas.BottomProperty, animation);
        }

        private void imgLogo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetPage(MainWindowPage.Protection, false);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                ctrlProtection.StopAnimation();
            }
            else
            {
                if (currentPage == MainWindowPage.Protection)
                {
                    ctrlProtection.StartAnimation();
                }
            }
        }

        private void disconnectTimer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowDisconnectTimerBanner();
        }

        public void AddDevModeClick()
        {
            if (DevMode)
            {
                return;
            }

            devModeClicks++;

            if (devModeClicks >= 10)
            {
                DevPasswordWindow devPasswordWindow = new DevPasswordWindow();
                devPasswordWindow.Owner = Window.GetWindow(this);
                var result = devPasswordWindow.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    DevMode = true;
                    Logger.Log.Info("Dev UI mode is on");
                }
                devModeClicks = 0;
            }
        }

        public void ResetDevModeClicks()
        {
            devModeClicks = 0;
        }


    }

    public enum MainWindowPage
    {
        None,
        SignIn,
        SignUp,
        Settings,
        Recovery,
        Recovered,
        Protection,
        IpAddress,
        SelectList
    }
}
