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
    /// Логика взаимодействия для DevPasswordWindow.xaml
    /// </summary>
    public partial class DevPasswordWindow : Window
    {
        public DevPasswordWindow()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = pbPassword.Password != null && pbPassword.Password.Equals(Config.DevModePassword);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
