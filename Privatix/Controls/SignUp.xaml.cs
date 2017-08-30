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
using Facebook;
using Privatix.Core;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для SignUp.xaml
    /// </summary>
    public partial class SignUp : UserControl
    {
        public SignUp()
        {
            InitializeComponent();
        }

        private void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            string email = tbEmail.Text;
            string password = (string)pbPassword.Password;
            if (!SiteConnector.IsValidEmail(email))
            {
                emailAvailable.Show("This email is incorrect", false);
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                emailAvailable.Show("Password is incorrect", false);
                return;
            }

            if (SyncConnector.Instance.IsConnected)
            {
                if (!App.ShowWarningToCloseConnection())
                {
                    return;
                }
            }

            MainWindow mainWindow = MainWindow.Instance;
            mainWindow.DisableControls();

            bool registered = false;
            Task<bool> task = new Task<bool>(() =>
            {
                return SiteConnector.Instance.PostRegistration(email, password, out registered);
            });
            task.Start();
            var UISyncContext = TaskScheduler.FromCurrentSynchronizationContext();
            task.ContinueWith(t =>
            {
                if (registered)
                {
                    mainWindow.SetPage(MainWindowPage.Protection, false);
                        

                    //ThreadPool.QueueUserWorkItem(o =>
                    //{
                    //    App.Site.PostMetrics(MetricsType.Registration);
                    //}
                    //);
                    GoogleAnalyticsApi.TrackEvent("signup", "win", "email");
                }
                else
                {
                    emailAvailable.Show("This email is already registered, please ", false, true, false);
                }

                mainWindow.EnableControls();
            }, UISyncContext);

        }

        private void btnSignUpFacebook_Click(object sender, RoutedEventArgs e)
        {
            FacebookAuthenticationWindow dialog = new FacebookAuthenticationWindow();
            if (dialog.ShowDialog() == true)
            {
                string accessToken = dialog.AccessToken;

                if (SyncConnector.Instance.IsConnected)
                {
                    if (!App.ShowWarningToCloseConnection())
                    {
                        return;
                    }
                }

                MainWindow mainWindow = MainWindow.Instance;
                mainWindow.DisableControls();
                bool authorized = false;
                string message = "";
                Task<bool> task = new Task<bool>(() =>
                {
                    try
                    {
                        FacebookClient fb = new FacebookClient(accessToken);
                        dynamic me = fb.Get("me", new { fields = new[] { "id", "email" } });
                        string email = me.email;

                        return SiteConnector.Instance.PostOAuth(email, accessToken, OAuthProvider.Facebook, out authorized, out message);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                        return false;
                    }

                });
                task.Start();
                var UISyncContext = TaskScheduler.FromCurrentSynchronizationContext();
                task.ContinueWith(t =>
                {
                    if (authorized)
                    {
                        mainWindow.SetPage(MainWindowPage.Protection, false);

                        //ThreadPool.QueueUserWorkItem(o =>
                        //{
                        //    App.Site.PostMetrics(MetricsType.Registration);
                        //}
                        //);
                        GoogleAnalyticsApi.TrackEvent("signup", "win", "fb");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(message))
                        {
                            emailAvailable.Show(message, false);
                        }
                        else
                        {
                            emailAvailable.Show("Unknown error", false);
                        }  
                    }

                    mainWindow.EnableControls();
                }, UISyncContext);
            }
        }

        private void btnSignUpGoogle_Click(object sender, RoutedEventArgs e)
        {
            GoogleAuthenticationWindow dialog = new GoogleAuthenticationWindow();
            if (dialog.ShowDialog() == true)
            {
                string code = dialog.Code;

                if (SyncConnector.Instance.IsConnected)
                {
                    if (!App.ShowWarningToCloseConnection())
                    {
                        return;
                    }
                }

                MainWindow mainWindow = MainWindow.Instance;
                mainWindow.DisableControls();
                bool authorized = false;
                string message = "";
                Task<bool> task = new Task<bool>(() =>
                {
                    try
                    {
                        string accessToken;
                        string idToken;
                        string email;
                        if (!SiteConnector.GetGoogleAuthToken(code, out accessToken, out idToken) ||
                            !SiteConnector.GetGoogleUserEmail(idToken, out email))
                        {
                            return false;
                        }

                        return SiteConnector.Instance.PostOAuth(email, idToken, OAuthProvider.Google, out authorized, out message);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                        return false;
                    }

                });
                task.Start();
                var UISyncContext = TaskScheduler.FromCurrentSynchronizationContext();
                task.ContinueWith(t =>
                {
                    if (authorized)
                    {
                        mainWindow.SetPage(MainWindowPage.Protection, false);

                        //ThreadPool.QueueUserWorkItem(o =>
                        //{
                        //    App.Site.PostMetrics(MetricsType.Registration);
                        //}
                        //);
                        GoogleAnalyticsApi.TrackEvent("signup", "win", "gplus");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(message))
                        {
                            emailAvailable.Show(message, false);
                        }
                        else
                        {
                            emailAvailable.Show("Unknown error", false);
                        }  
                    }

                    mainWindow.EnableControls();
                }, UISyncContext);
            }
        }

        private void tbPrivacyPolicy_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(Config.PrivacyUrl);
        }

        private void tbTermOfService_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(Config.TermOfUseUrl);
        }

        private void tbEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            string email = tbEmail.Text;

            if (string.IsNullOrEmpty(email))
            {
                return;
            }

            if (!SiteConnector.IsValidEmail(email))
            {
                emailAvailable.Show("This email is incorrect", false);
                return;
            }

            bool emailFound = false;
            Task<bool> task = new Task<bool>(() =>
                {
                    return SiteConnector.Instance.PostCheckMail(email, out emailFound);
                });
            task.Start();
            var UISyncContext = TaskScheduler.FromCurrentSynchronizationContext();
            task.ContinueWith(t =>
                {
                    if (!t.Result)
                    {
                        emailAvailable.Show("Connection error", false);
                    }
                    else
                    {
                        if (emailFound)
                        {
                            emailAvailable.Show("This email is already registered, please ", false, true, false);
                        }
                        else
                        {
                            emailAvailable.Show("This email is available", true);
                        }
                    }
                }, UISyncContext);
        }

        private void tbEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            emailAvailable.Hide();
        }
    }
}
