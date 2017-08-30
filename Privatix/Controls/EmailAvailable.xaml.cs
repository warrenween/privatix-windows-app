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

namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для EmailAvailable.xaml
    /// </summary>
    public partial class EmailAvailable : UserControl
    {
        public EmailAvailable()
        {
            InitializeComponent();
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
            DependencyProperty.Register("IsAvailable", typeof(bool), typeof(EmailAvailable),
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
            DependencyProperty.Register("Text", typeof(string), typeof(EmailAvailable),
                                        new FrameworkPropertyMetadata(""));
        #endregion

        #region dependency property bool ShowLogIn
        public bool ShowLogIn
        {
            get
            {
                return (bool)GetValue(ShowLogInProperty);
            }
            set
            {
                SetValue(ShowLogInProperty, value);
            }
        }

        public static readonly DependencyProperty ShowLogInProperty =
            DependencyProperty.Register("ShowLogIn", typeof(bool), typeof(EmailAvailable),
                                        new FrameworkPropertyMetadata(false));
        #endregion

        #region dependency property bool ShowSignUp
        public bool ShowSignUp
        {
            get
            {
                return (bool)GetValue(ShowSignUpProperty);
            }
            set
            {
                SetValue(ShowSignUpProperty, value);
            }
        }

        public static readonly DependencyProperty ShowSignUpProperty =
            DependencyProperty.Register("ShowSignUp", typeof(bool), typeof(EmailAvailable),
                                        new FrameworkPropertyMetadata(false));
        #endregion

        public void Show(string text, bool isAvailable, bool showLogIn = false, bool showSignUp = false)
        {
            this.ShowLogIn = showLogIn;
            this.ShowSignUp = showSignUp;
            this.Text = text;
            this.IsAvailable = isAvailable;
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Hidden;
        }

        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SetPage(MainWindowPage.SignIn, true);
        }

        private void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SetPage(MainWindowPage.SignUp, true);
        }
    }
}
