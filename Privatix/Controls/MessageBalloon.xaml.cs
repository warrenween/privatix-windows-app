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
    /// Логика взаимодействия для MessageBalloon.xaml
    /// </summary>
    public partial class MessageBalloon : UserControl
    {
        private string link = "";


        public string Link
        {
            get { return link; }
            set
            {
                link = value;
                if (string.IsNullOrEmpty(link))
                {
                    tbText.Cursor = Cursors.Arrow;
                }
                else
                {
                    tbText.Cursor = Cursors.Hand;
                }
            }
        }


        public MessageBalloon()
        {
            InitializeComponent();
        }

        private void tbText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(link))
            {
                if (link.ToLower().Contains("privatix://protection"))
                {
                    App.ShowMainWindowPage(MainWindowPage.Protection);
                }
                else
                {
                    try
                    {
                        Process.Start(link);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                    }
                }
            }
        }
    }
}
