using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;


namespace Privatix
{
    /// <summary>
    /// Логика взаимодействия для TrayWindow.xaml
    /// </summary>
    public partial class TrayWindow : Window
    {
        public TrayWindow()
        {
            InitializeComponent();
        }

        public void ShowStartupWindow()
        {
            StartWindow startWindow = Application.Current.MainWindow as StartWindow;
            if (startWindow == null)
            {
                startWindow = new StartWindow();
                Application.Current.MainWindow = startWindow;
            }

            startWindow.Visibility = Visibility.Visible;
            //startWindow.Show();
            startWindow.Activate();
            startWindow.ShowInTaskbar = true;
            //startWindow.WindowState = WindowState.Normal;
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            //send 'exit' only if not closed window
            Window window = Application.Current.MainWindow;
            if (window != null && window.ShowInTaskbar)
            {
                Task eventTask = new Task(() => GoogleAnalyticsApi.TrackEventAndWait("exit", "win"));
                eventTask.Start();
                eventTask.Wait(3000);
            }

            SyncConnector.Instance.Disconnect(true);
            try { Guard.Instance.OnDisconnected(); }
            catch { }
            try { DnsChanger.Instance.DisableDnsLeakGuard(null); }
            catch { }
            try { TimeZoneChanger.Instance.RestoreTimeZone(); }
            catch { }

            Application.Current.Shutdown(0);
        }

        public void ShowActiveWindow()
        {
            if (Dispatcher.CheckAccess())
            {
                Window window = Application.Current.MainWindow;
                if (window != null)
                {
                    //send open only if window closed
                    if (!window.ShowInTaskbar)
                    {
                        GoogleAnalyticsApi.TrackEvent("open", "win");
                    }

                    window.ShowInTaskbar = true;
                    window.Visibility = Visibility.Visible;
                    window.WindowState = WindowState.Normal;                    
                    window.Activate();                    
                }

                return;
            }

            Dispatcher.Invoke(new SimpleHandler(ShowActiveWindow), DispatcherPriority.Normal);
        }

        private void miShow_Click(object sender, RoutedEventArgs e)
        {
            ShowActiveWindow();
        }

        private void tiTaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowActiveWindow();
        }

        private void miMyIp_Click(object sender, RoutedEventArgs e)
        {
            App.ShowMainWindowPage(MainWindowPage.IpAddress);
            GoogleAnalyticsApi.TrackEvent("ipcheck_screen", "win");
        }

        private void miSettings_Click(object sender, RoutedEventArgs e)
        {
            App.ShowMainWindowPage(MainWindowPage.Settings);
            GoogleAnalyticsApi.TrackEvent("settings", "win");
        }
    }
}
