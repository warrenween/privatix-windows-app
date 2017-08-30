using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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


namespace Privatix
{
    /// <summary>
    /// Логика взаимодействия для LockWindow.xaml
    /// </summary>
    public partial class LockWindow : Window
    {
        private int counter = 999;
        private int startCounter = 999;
        private Timer timer;
        private bool canClose = false;
        public int Ttl = 5;

        
        public LockWindow()
        {
            InitializeComponent();
        }

        private void btnGoPremium_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Config.LockPremiumUrl);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Could not open browser", ex);
                MessageBox.Show("Could not open browser", "Error", MessageBoxButton.OK);
            }
            
            GoogleAnalyticsApi.TrackEvent("premium", "win");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
            }            
        }

        public void Start(int ttl)
        {
            startCounter = Config.Random.Next(500, 600);
            counter = startCounter;
            UpdateCounter();

            MainWindow mainWindow = MainWindow.Instance;
            this.Left = mainWindow.Left;
            this.Top = mainWindow.Top;
            Application.Current.MainWindow = this;
            mainWindow.Hide();

            Ttl = ttl;
            timer = new Timer(CounterTimer);
            timer.Change(1000, Timeout.Infinite);
        }

        private void UpdateCounter()
        {
            string strCounter = counter.ToString("D3");

            tbNumber1.Text = strCounter[0].ToString();
            tbNumber2.Text = strCounter[1].ToString();
            tbNumber3.Text = strCounter[2].ToString();
        }

        public void CounterTimer(object state)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new TimerCallback(CounterTimer),
                                  DispatcherPriority.Normal,
                                  new object[] { state });
                return;
            }

            counter -= (int)(((float)startCounter / Ttl) * ((float)Config.Random.Next(80, 130) / 100));

            if (counter <= 0)
            {
                MainWindow mainWindow = MainWindow.Instance;
                mainWindow.Left = this.Left;
                mainWindow.Top = this.Top;
                Application.Current.MainWindow = mainWindow;
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();

                timer.Change(Timeout.Infinite, Timeout.Infinite);
                canClose = true;
                this.Close();
                return;
            }

            UpdateCounter();
            timer.Change(1000, Timeout.Infinite);
        }
    }
}
