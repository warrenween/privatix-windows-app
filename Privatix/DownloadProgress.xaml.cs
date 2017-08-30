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

namespace Privatix
{
    /// <summary>
    /// Логика взаимодействия для DownloadProgress.xaml
    /// </summary>
    public partial class DownloadProgress : Window
    {
        public DownloadProgress()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Updater.CancelDownload();
            Close();
        }

        public void ChangeProgress(double progress)
        {
            pbDownload.Value = progress;
        }
    }
}
