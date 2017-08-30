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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для DisconnectTimer.xaml
    /// </summary>
    public partial class DisconnectTimer : UserControl
    {
        private DateTime endTime = DateTime.MinValue;
        private DispatcherTimer timer;


        public DisconnectTimer()
        {
            InitializeComponent();
        }

        #region dependency property bool IsWorking
        public bool IsWorking
        {
            get
            {
                return (bool)GetValue(IsWorkingProperty);
            }
            set
            {
                SetValue(IsWorkingProperty, value);
            }
        }

        public static readonly DependencyProperty IsWorkingProperty =
            DependencyProperty.Register("IsWorking", typeof(bool), typeof(DisconnectTimer),
                                        new FrameworkPropertyMetadata(false));
        #endregion

        #region dependency property bool IsAlarming
        public bool IsAlarming
        {
            get
            {
                return (bool)GetValue(IsAlarmingProperty);
            }
            set
            {
                SetValue(IsAlarmingProperty, value);
            }
        }

        public static readonly DependencyProperty IsAlarmingProperty =
            DependencyProperty.Register("IsAlarming", typeof(bool), typeof(DisconnectTimer),
                                        new FrameworkPropertyMetadata(false));
        #endregion

        #region dependency property string Text
        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(DisconnectTimer),
                                        new FrameworkPropertyMetadata(""));
        #endregion

        #region dependency property string TextColor
        public string TextColor
        {
            get
            {
                return (string)GetValue(TextColorProperty);
            }
            set
            {
                SetValue(TextColorProperty, value);
            }
        }

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register("TextColor", typeof(Brush), typeof(DisconnectTimer),
                                        new FrameworkPropertyMetadata(Brushes.White));
        #endregion


        public void Start(int timerSeconds)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new VoidIntHandler(Start),
                                  DispatcherPriority.Normal,
                                  new object[] { timerSeconds });
                return;
            }

            if (timerSeconds <= 0)
                return;

            endTime = DateTime.Now.AddSeconds(timerSeconds);

            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Tick += timer_Tick;
                timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            }
            if (!timer.IsEnabled)
            {
                timer.Start();
            }

            IsAlarming = false;
            timer_Tick(null, null);
            IsWorking = true;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = endTime - DateTime.Now;
            if (elapsed.TotalSeconds < 0)
            {
                Stop();
                SyncConnector.Instance.BeginDisconnect(true);
                return;
            }
            if (elapsed.TotalSeconds < 5 * 60 + 1) //5 minutes and less
            {
                if (!IsAlarming)
                {
                    IsAlarming = true;
                    App app = ((App)App.Current);
                    app.ShowTrayNotify("Time to disconnect : 5min", "privatix://protection", 15);
                    MainWindow mainWindow = app.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.ShowDisconnectTimerBanner();
                    }
                }                
            }

            Text = string.Format("{0:D2}:{1:D2}", elapsed.Hours * 60 + elapsed.Minutes, elapsed.Seconds);
        }

        public void Stop()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new SimpleHandler(Stop), DispatcherPriority.Normal);
                return;
            }

            IsWorking = false;
            IsAlarming = false;

            if (timer != null)
            {
                timer.Stop();
            }
        }
    }
}
