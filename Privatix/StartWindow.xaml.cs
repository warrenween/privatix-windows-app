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
using Privatix.Core;


namespace Privatix
{
    /// <summary>
    /// Логика взаимодействия для StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }

        public void OnGetProtectedClick(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = MainWindow.Instance;
            mainWindow.Left = this.Left;
            mainWindow.Top = this.Top;
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();

            this.Close();
        }

        private void OnLogInClick(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = MainWindow.Instance;
            mainWindow.Left = this.Left;
            mainWindow.Top = this.Top;
            mainWindow.SetPage(MainWindowPage.SignIn, true);
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();

            this.Close();
        }

        public void ShowControls()
        {
            tbStatus.Visibility = Visibility.Collapsed;
            btnReconnect.Visibility = Visibility.Collapsed;
            btnGetProtected.Visibility = Visibility.Visible;
            tbAlreadyHave.Visibility = Visibility.Visible;
            btnLogIn.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Minimized;

            GoogleAnalyticsApi.TrackEvent("exit", "win");
        }

        public void ShowReconnect()
        {
            btnReconnect.Visibility = Visibility.Visible;
        }

        private void OnReconnectClick(object sender, RoutedEventArgs e)
        {
            btnReconnect.Visibility = Visibility.Collapsed;
            App.ReconnectEvent.Set();
        }
    }
}
