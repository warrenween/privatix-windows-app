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
    /// Логика взаимодействия для FacebookAuthenticationWindow.xaml
    /// </summary>
    public partial class FacebookAuthenticationWindow : Window
    {
        //The access token retrieved from facebook's authentication
        public string AccessToken { get; set; }


        public FacebookAuthenticationWindow()
        {
            InitializeComponent();

            this.Loaded += (object sender, RoutedEventArgs e) =>
            {
                //Add the message hook in the code behind since I got a weird bug when trying to do it in the XAML
                webBrowser.MessageHook += webBrowser_MessageHook;

                //Delete the cookies since the last authentication
                DeleteFacebookCookie();

                //Create the destination URL
                var destinationURL = String.Format("https://www.facebook.com/dialog/oauth?client_id={0}&scope={1}&display=popup&redirect_uri={2}&response_type=token",
                   Config.FacebookAppId, //client_id
                   Config.FacebookScope, //scope
                   Config.FacebookRedirectUri
                );
                webBrowser.Navigate(destinationURL);
            };
        }

        private void webBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //If the URL has an access_token, grab it and walk away...
            var url = e.Uri.Fragment;
            if (url.Contains("access_token") && url.Contains("#"))
            {
                url = (new System.Text.RegularExpressions.Regex("#")).Replace(url, "?", 1);
                AccessToken = System.Web.HttpUtility.ParseQueryString(url).Get("access_token");
                DialogResult = true;
                this.Close();
            }
        }

        private void webBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.Uri.LocalPath == "/r.php")
            {
                MessageBox.Show("To create a new account go to www.facebook.com", "Could Not Create Account", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
            else if (e.Uri.AbsoluteUri.Contains("error=access_denied"))
            {
                this.Close();
            }
        }

        private void DeleteFacebookCookie()
        {
            //Set the current user cookie to have expired yesterday
            string cookie = String.Format("c_user=; expires={0:R}; path=/; domain=.facebook.com", DateTime.UtcNow.AddDays(-1).ToString("R"));
            Application.SetCookie(new Uri("https://www.facebook.com"), cookie);
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
