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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Privatix.Core;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для PasswordRecovery.xaml
    /// </summary>
    public partial class PasswordRecovery : UserControl
    {
        public PasswordRecovery()
        {
            InitializeComponent();
        }

        private void btnRecover_Click(object sender, RoutedEventArgs e)
        {
            string email = tbEmail.Text;
            if (!SiteConnector.IsValidEmail(email))
            {
                emailAvailable.Show("This email is incorrect", false);
                return;
            }

            MainWindow mainWindow = MainWindow.Instance;
            mainWindow.DisableControls();

            bool recovered = false;
            Task<bool> task = new Task<bool>(() =>
            {
                return SiteConnector.Instance.PostRecover(email, out recovered);
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
                    if (recovered)
                    {
                        mainWindow.SetPage(MainWindowPage.Recovered, false);
                        GoogleAnalyticsApi.TrackEvent("pass_recovery", "win");
                    }
                    else
                    {
                        emailAvailable.Show("This email is not registered", false);
                    }
                }

                mainWindow.EnableControls();
            }, UISyncContext);
        }
    }
}
