using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Privatix.Core;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для Banner.xaml
    /// </summary>
    public partial class Banner : UserControl
    {
        public string Link;

        public Banner()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.CloseBanner(null);
        }

        private void tbText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(Link))
            {
                try
                {
                    Process.Start(Link);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
                GoogleAnalyticsApi.TrackEvent("bannerclick", "win");
            }
        }
    }
}
