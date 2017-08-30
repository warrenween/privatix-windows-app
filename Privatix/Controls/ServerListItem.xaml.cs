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
    /// Логика взаимодействия для ServerListItem.xaml
    /// </summary>
    public partial class ServerListItem : UserControl
    {
        private InternalServerEntry entry;


        public ServerListItem()
        {
            InitializeComponent();

        }

        public InternalServerEntry ServerEntry
        {
            get 
            {
                return entry;
            }
            set
            {
                entry = value;

                try
                {
                    Uri uri = new Uri("pack://application:,,,/Privatix.Resources;component/small_flags/" + entry.CountryCode + ".png");
                    try
                    {
                        Image = new BitmapImage(uri);
                    }
                    catch
                    {
                        try
                        {
                            Image = new BitmapImage(new Uri("pack://application:,,,/Privatix.Resources;component/small_flags/none.png"));
                        }
                        catch
                        {
                            Image = new BitmapImage();
                        }                        
                    }

                    Text = entry.DisplayName;
                }
                catch (Exception ex)
                {
                    Image = new BitmapImage();
                    Text = "";

                    Logger.Log.Error(ex.Message, ex);
                }                
            }
        }

        #region dependency property bool IsAvailable
        public bool IsAvailable
        {
            get
            {
                return (bool)GetValue(IsAvailableProperty);
            }
            set
            {
                SetValue(IsAvailableProperty, value);
            }
        }

        public static readonly DependencyProperty IsAvailableProperty =
            DependencyProperty.Register("IsAvailable", typeof(bool), typeof(ServerListItem),
                                        new FrameworkPropertyMetadata(true));
        #endregion

        #region dependency property bool IsChecked
        public bool IsChecked
        {
            get
            {
                return (bool)GetValue(IsCheckedProperty);
            }
            set
            {
                SetValue(IsCheckedProperty, value);
            }
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ServerListItem),
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
            DependencyProperty.Register("Text", typeof(string), typeof(ServerListItem),
                                        new FrameworkPropertyMetadata(""));
        #endregion

        #region dependency property ImageSource Image
        public ImageSource Image
        {
            get
            {
                return (ImageSource)GetValue(ImageProperty);
            }
            set
            {
                SetValue(ImageProperty, value);
            }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(ServerListItem),
                                        new FrameworkPropertyMetadata(null));
        #endregion
    }
}
