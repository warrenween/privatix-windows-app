using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для SignIn.xaml
    /// </summary>
    public partial class SignIn : UserControl
    {
        public SignIn()
        {
            InitializeComponent();
        }

        private void btnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SetPage(MainWindowPage.Recovery, true);
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
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

            bool authorized = false;
            string message = "";
            Task<bool> task = new Task<bool>(() =>
            {
                return SiteConnector.Instance.PostAuthorization(email, password, out authorized, out message);
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
                    //    App.Site.PostMetrics(MetricsType.Authorization);
                    //}
                    //);
                    GoogleAnalyticsApi.TrackEvent("login", "win", "email");
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

        private void btnLoginFacebook_Click(object sender, RoutedEventArgs e)
        {
            FacebookAuthenticationWindow dialog = new FacebookAuthenticationWindow();
            if (dialog.ShowDialog() == true)
            {
                string accessToken = dialog.AccessToken;

                Logger.Log.Info("Facebook access token = " + accessToken);

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

                        Logger.Log.Info("Facebook email = " + email);

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
                        //    App.Site.PostMetrics(MetricsType.Authorization);
                        //}
                        //);
                        GoogleAnalyticsApi.TrackEvent("login", "win", "fb");
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

        private void btnLoginGoogle_Click(object sender, RoutedEventArgs e)
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
                    if (!t.Result)
                    {
                        emailAvailable.Show("Authorization error", false);
                    }
                    else
                    {
                        if (authorized)
                        {
                            mainWindow.SetPage(MainWindowPage.Protection, false);

                            //ThreadPool.QueueUserWorkItem(o =>
                            //{
                            //    App.Site.PostMetrics(MetricsType.Authorization);
                            //}
                            //);
                            GoogleAnalyticsApi.TrackEvent("login", "win", "gplus");
                        }
                        else
                        {
                            emailAvailable.Show("Incorrect email or password", false);
                        }
                    }

                    mainWindow.EnableControls();
                }, UISyncContext);
            }
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
                        emailAvailable.Show("This email is correct", true);
                    }
                    else
                    {
                        emailAvailable.Show("This email is not registered, please ", false, false, true);
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
