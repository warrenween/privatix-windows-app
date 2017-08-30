using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для IpAddress.xaml
    /// </summary>
    public partial class IpAddress : UserControl
    {
        public IpAddress()
        {
            InitializeComponent();

            GlobalEvents.OnIpInfoUpdated += OnIpAdressChanged;

            UpdateIpAdressInfo();
        }

        public void UpdateIpAdressInfo()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new SimpleHandler(UpdateIpAdressInfo), DispatcherPriority.Normal);
                return;
            }

            SiteConnector site = SiteConnector.Instance;

            tbIpAddress.Text = string.IsNullOrEmpty(site.CurrentIp) ? "UNKNOWN" : site.CurrentIp;

            if (tbIpAddress.Text.Contains("Checking"))
            {
                tbIpLabel.Text = "";
                tbCountry.Text = "";
                tbCountryLabel.Text = "";

                SetFlagImage("unknown");
            }
            else
            {
                tbIpLabel.Text = "IP: ";
                tbCountryLabel.Text = "\nCOUNTRY: ";

                string currentCountry = site.CurrentCountry;
                if (string.IsNullOrEmpty(currentCountry))
                {
                    currentCountry = "UNKNOWN";
                }

                try
                {
                    RegionInfo regionInfo = new RegionInfo(currentCountry);
                    tbCountry.Text = regionInfo.EnglishName;
                }
                catch
                {
                    tbCountry.Text = currentCountry;
                }

                SetFlagImage(currentCountry);
            }            
        }

        private void SetFlagImage(string name)
        {
            Uri uri = new Uri(string.Format("pack://application:,,,/Privatix.Resources;component/big_flags/{0}.png", name));
            try
            {
                imgIpFlag.Source = new BitmapImage(uri);
            }
            catch
            {
                imgIpFlag.Source = new BitmapImage();
            }
        }

        private void OnIpAdressChanged(object sender, EventArgs args)
        {
            UpdateIpAdressInfo();
        }

        private void btnClickForMore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Config.IpAdressDetailsUrl);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Could not open browser", ex);
                MessageBox.Show("Could not open browser", "Error", MessageBoxButton.OK);
            }

            GoogleAnalyticsApi.TrackEvent("ipcheck_click", "win");
        }
    }
}
