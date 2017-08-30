using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        bool initialized;

        public Settings()
        {
            initialized = false;

            InitializeComponent();

            chbStartWithWindows.IsChecked = Config.AutoRun;
            chbDnsLeak.IsChecked = Config.DnsLeak;
            chbConnectionGuard.IsChecked = Config.ConnectionGuard;
            chbNetworkAlert.IsChecked = Config.NetworkAlert;
            chbAdjustSystemTime.IsChecked = Config.AdjustSystemTime;
            chbAutoUpdate.IsChecked = Config.AutoUpdate;

#if _DEV_API_
            tbVersion.Text = "Version: " + Config.Version + " dev";
#elif _STAGE_API_
            tbVersion.Text = "Version: " + Config.Version + " stage";
#else
            tbVersion.Text = "Version: " + Config.Version;
#endif

            GlobalEvents.OnSubscriptionUpdated += OnSubscriptionChanged;
            FillSubscription();

            initialized = true;
        }

        private void chbStartWithWindows_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                Config.AutoRun = chbStartWithWindows.IsChecked.Value;
            }            
        }

        private void chbDnsLeak_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                Config.DnsLeak = chbDnsLeak.IsChecked.Value;
                if (SyncConnector.Instance.IsConnected)
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
                    else
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
                }                
            }
        }

        private void chbConnectionGuard_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                Config.ConnectionGuard = chbConnectionGuard.IsChecked.Value;

                if (Config.ConnectionGuard)
                {
                    if (!Guard.Instance.IsGuardEnabled)
                    {
                        if (SyncConnector.Instance.IsConnected)
                        {
                            MessageBox.Show("You need reconnect for enable Connection Guard");
                        }
                    }
                }
                else
                {
                    //if (Guard.Instance.IsGuardEnabled)
                    //{
                    //    Guard.Instance.DisableGuard();
                    //}
                }
            }
        }

        private void chbAutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                Config.AutoUpdate = chbAutoUpdate.IsChecked.Value;

                if (Config.AutoUpdate)
                {
                    ThreadPool.QueueUserWorkItem(o => { Updater.Start(); });
                }
                else
                {
                    Updater.Stop();
                }
            }
        }

        //private void btnLogIn_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.Instance.SetPage(MainWindowPage.SignIn, true);
        //}

        //private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        //{
        //    SiteConnector site = App.Site;
        //    if (site == null)
        //    {
        //        return;
        //    }

        //    MainWindow mainWindow = MainWindow.Instance;
        //    mainWindow.DisableControls();

        //    bool recovered = false;
        //    Task<bool> task = new Task<bool>(() =>
        //    {
        //        return site.PostRecover(Config.LastEmail, out recovered);
        //    });
        //    task.Start();
        //    var UISyncContext = TaskScheduler.FromCurrentSynchronizationContext();
        //    task.ContinueWith(t =>
        //    {
        //        if (!t.Result)
        //        {
        //            MessageBox.Show("Connection error", "Error", MessageBoxButton.OK);
        //        }
        //        else
        //        {
        //            if (recovered)
        //            {
        //                MessageBox.Show("Check email for continue...", "Change password", MessageBoxButton.OK);
        //            }
        //            else
        //            {
        //                MessageBox.Show("This email is not registered", "Error", MessageBoxButton.OK);
        //            }
        //        }

        //        mainWindow.EnableControls();
        //    }, UISyncContext);
        //}

        //private void btnSignUp_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.Instance.SetPage(MainWindowPage.SignUp, true);
        //}

        private void OnSubscriptionChanged(object sender, EventArgs args)
        {
            FillSubscription();
        }

        public void FillSubscription()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new SimpleHandler(FillSubscription), DispatcherPriority.Normal);
                return;
            }

            SiteConnector site = SiteConnector.Instance;
            if (site.IsAuthorized)
            {
                tbLogInOrSignUp.Visibility = Visibility.Hidden;
                btnLogout.Visibility = Visibility.Visible;
                tbEmailLabel.Visibility = Visibility.Visible;
                tbEmail.Visibility = Visibility.Visible;

                tbEmail.Text = site.Email;
            }
            else
            {
                tbEmailLabel.Visibility = Visibility.Hidden;
                tbEmail.Visibility = Visibility.Hidden;
                btnLogout.Visibility = Visibility.Hidden;
                tbLogInOrSignUp.Visibility = Visibility.Visible;
            }

            tbAccountType.Text = site.Plan.ToUpper();
            if (site.IsFree)
            {
                tbExpirationDate.Text = "Lifetime";
            }
            else
            {
                tbExpirationDate.Text = site.PremiumEnds.ToString("d");
            }
        }

        private void btnLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(System.IO.Path.Combine(Config.LogPath, Config.LogFilename));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open logs", "Error", MessageBoxButton.OK);
                Logger.Log.Error(ex.Message, ex);
            }
        }

        private void btnGetHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.HelpUrl);
        }

        private void btnTermsOfUse_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.TermOfUseUrl);
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (SyncConnector.Instance.IsConnected)
            {
                if (!App.ShowWarningToCloseConnection())
                {
                    return;
                }
            }

            MainWindow mainWindow = MainWindow.Instance;
            mainWindow.DisableControls();

            Task<bool> task = new Task<bool>(() =>
            {
                return SiteConnector.Instance.PostActivation();
            });
            task.Start();
            var UISyncContext = TaskScheduler.FromCurrentSynchronizationContext();
            task.ContinueWith(t =>
            {
                if (!t.Result)
                {
                    MessageBox.Show("Server connection error", "Error", MessageBoxButton.OK);
                }

                mainWindow.EnableControls();
            }, UISyncContext);
        }

        private void chbNetworkAlert_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                Config.NetworkAlert = chbNetworkAlert.IsChecked.Value;

                MainWindow.Instance.AddDevModeClick();
            }            
        }

        private void chbAdjustSystemTime_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                Config.AdjustSystemTime = chbAdjustSystemTime.IsChecked.Value;

                if (SyncConnector.Instance.IsConnected)
                {
                    if (Config.AdjustSystemTime)
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            try
                            {
                                TimeZoneChanger.Instance.ChangeTimeZone(SyncConnector.Instance.CurrentEntry.CurrentTimezone);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        );
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            TimeZoneChanger.Instance.RestoreTimeZone();
                        }
                        );
                    }
                }
            }
        }

        private void tbLogIn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.Instance.SetPage(MainWindowPage.SignIn, true);
        }

        private void tbSignUp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.Instance.SetPage(MainWindowPage.SignUp, true);
        }
    }
}
