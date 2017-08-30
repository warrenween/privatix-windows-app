using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Privatix.Core;


namespace Privatix
{
    /// <summary>
    /// Логика взаимодействия для GoogleAuthenticationWindow.xaml
    /// </summary>
    public partial class GoogleAuthenticationWindow : Window
    {
        public string Code { get; set; }


        public GoogleAuthenticationWindow()
        {
            InitializeComponent();

            this.Loaded += (object sender, RoutedEventArgs e) =>
            {
                //Add the message hook in the code behind since I got a weird bug when trying to do it in the XAML
                webBrowser.MessageHook += webBrowser_MessageHook;

                //Create the destination URL
                var destinationURL = String.Format("https://accounts.google.com/o/oauth2/v2/auth?scope={1}&state=%2Fprofile&redirect_uri={2}&response_type=code&client_id={0}",
                   Config.GoogleClientId, //client_id
                   Config.GoogleScope, //scope
                   Config.GoogleRedirectUri
                );
                webBrowser.Navigate(destinationURL);
            };
        }

        private void webBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            
        }

        private void webBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            Uri redirectUri = new Uri(Config.GoogleRedirectUri);
            var url = e.Uri;
            if (url.Host.ToLower() == redirectUri.Host.ToLower())
            {
                try
                {
                    Code = System.Web.HttpUtility.ParseQueryString(url.AbsoluteUri).Get("code");
                    DialogResult = true;
                }
                finally
                {
                    e.Cancel = true;
                    this.Close();
                }
            }

            if (e.Uri.LocalPath == "/RecoverAccount")
            {
                MessageBox.Show("To recover account go to accounts.google.com", "Could Not Recover Account", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
            else if (e.Uri.LocalPath == "/SignUp")
            {
                MessageBox.Show("To create a new account go to accounts.google.com", "Could Not Create Account", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
            else if (e.Uri.LocalPath == "/intl/ru/policies/privacy/embedded/")
            {
                e.Cancel = true;
            }
            else if (e.Uri.LocalPath == "/intl/ru/policies/terms/embedded/")
            {
                e.Cancel = true;
            }
        }

        private IntPtr webBrowser_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //msg = 130 is the last call for when the window gets closed on a window.close() in javascript
            if (msg == 130)
            {
                this.Close();
            }
            return IntPtr.Zero;
        }
    }
}
