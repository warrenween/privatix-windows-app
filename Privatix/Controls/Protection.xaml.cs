using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;
using Privatix.Core.Notification;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для Protection.xaml
    /// </summary>
    public partial class Protection : UserControl
    {
        private bool initializing = true;
        private bool protectionOn;
        private InternalServerEntry selectedServer = null;
        private DispatcherTimer showDisconnectButtonTimer;


        #region dependency property bool DevMode
        public bool DevMode
        {
            get
            {
                return (bool)GetValue(DevModeProperty);
            }
            set
            {
                SetValue(DevModeProperty, value);
            }
        }

        public static readonly DependencyProperty DevModeProperty =
            DependencyProperty.Register("DevMode", typeof(bool), typeof(Protection),
                                        new FrameworkPropertyMetadata(false));
        #endregion


        public Protection()
        {
            InitializeComponent();

            showDisconnectButtonTimer = new DispatcherTimer();
            showDisconnectButtonTimer.Tick += ShowDisconnectButton;
            showDisconnectButtonTimer.Interval = new TimeSpan(0, 0, 1);

            GlobalEvents.OnConnecting += GlobalEvents_OnConnecting;
            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnecting += GlobalEvents_OnDisconnecting;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
            GlobalEvents.OnEnded += GlobalEvents_OnEnded;
            GlobalEvents.OnChangeServer += OnServerChanged;
            GlobalEvents.OnSubscriptionUpdated += GlobalEvents_OnSubscriptionUpdated;
            FillSelectedServer();

            protectionOn = false;
            ShowProtectionOff();

            initializing = false;

            SetDefaultServer();
        }

        private void SetDefaultServer()
        {
            SiteConnector site = SiteConnector.Instance;
            InternalServerEntry entry = site.Servers.FirstOrDefault(o => (o.IsFree == true || !site.IsFree));
            selectedServer = entry;
            FillSelectedServer();
        }

        void GlobalEvents_OnSubscriptionUpdated(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new EventHandler(GlobalEvents_OnSubscriptionUpdated),
                                  DispatcherPriority.Normal, new object[] { sender, e });
                return;
            }

            SiteConnector site = SiteConnector.Instance;

            if (selectedServer == null)
            {
                SetDefaultServer();
                return;
            }
      
            foreach (InternalServerEntry entry in site.Servers)
            {
                if (entry == selectedServer)
                {
                    if (site.IsFree && !entry.IsFree)
                    {
                        if (entry.ConnectStatus == InternalStatus.Connected)
                        {
                            Logger.Log.Info("Connected server need premium. Do disconnect");
                            SyncConnector.Instance.BeginDisconnect(false, true);
                        }
                        selectedServer = null;
                        FillSelectedServer();
                    }

                    return;
                }
            }

            if (selectedServer.ConnectStatus == InternalStatus.Connected)
            {
                InternalServerEntry alter = site.FindAlternativeEntry(selectedServer);
                if (alter != null)
                {
                    selectedServer = alter;
                    FillSelectedServer();
                    return;
                }
                else
                {
                    Logger.Log.Info("Connected server not found in list. Do disconnect");
                    SyncConnector.Instance.BeginDisconnect(false, true);
                    selectedServer = null;
                    FillSelectedServer();
                    return;
                }
            }

            SetDefaultServer();     
        }

        void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            //Logger.Log.Info(Environment.StackTrace);

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new ServerEntryEventHandler(GlobalEvents_OnDisconnected),
                                  DispatcherPriority.Normal,
                                  new object[] {sender, args});

                Logger.Log.Info("End invoke");
                return;
            }

            if (!SyncConnector.Instance.ManualDisconnect && !SyncConnector.Instance.ChangingServer)
            {
                if (!SyncConnector.Instance.NodeChangeDisconnect)
                {
                    if ((!args.Entry.IsFree && App.Site.IsFree) 
                        || !SyncConnector.Instance.TryReconnect(args.Entry))
                    {
                        if (Guard.Instance.NeedShow)
                        {
                            ShowGuardDialog(args.Entry);
                        }
                    } 
                }
                else
                {
                    if (Guard.Instance.NeedShow)
                    {
                        ShowGuardDialog(args.Entry);
                    }
                }
            }

            ShowProtectionOff();
        }

        void GlobalEvents_OnEnded(object sender, ServerEntryEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new ServerEntryEventHandler(GlobalEvents_OnEnded),
                                  DispatcherPriority.Normal, new object[] { sender, args });
                return;
            }

            if (SyncConnector.Instance.CurrentEntry != null
                && SyncConnector.Instance.CurrentEntry.ConnectStatus != InternalStatus.Disconnected)
            {
                Logger.Log.Info("New connection starting, dont OFF");
                return;
            }

            if (!SyncConnector.Instance.ChangingServer)
            {
                MainWindow.Instance.EnableControls();
            }
        }

        void GlobalEvents_OnDisconnecting(object sender, ServerEntryEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new ServerEntryEventHandler(GlobalEvents_OnDisconnecting),
                                  DispatcherPriority.Normal, new object[] { sender, args });
                return;
            }

            MainWindow.Instance.DisableControls();
            tbStatus.Text = "\nDisconnecting...";
            tbTurnOn.Text = "";
        }

        void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new ServerEntryEventHandler(GlobalEvents_OnConnected),
                                  DispatcherPriority.Normal, new object[] { sender, args });
                return;
            }

            if (selectedServer != args.Entry)
            {
                Logger.Log.InfoFormat("selectedServer != args.Entry {0} {1}", selectedServer == null ? "(null)" : selectedServer.Country, args.Entry.Country);
                selectedServer = args.Entry;
                FillSelectedServer();
            }

            ShowProtectionOn();
            MainWindow.Instance.EnableControls();
        }

        void GlobalEvents_OnConnecting(object sender, ServerEntryEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new ServerEntryEventHandler(GlobalEvents_OnConnecting),
                                  DispatcherPriority.Normal, new object[] { sender, args });
                return;
            }

            MainWindow.Instance.DisableControls();
            if (DevMode)
            {

                tbStatus.Text = string.Format("Connecting via {0}\n to {1}", SyncConnector.Instance.CurrentProtocol.ToString(), args.Entry.CurrentDomen);
            }
            else
            {
                tbStatus.Text = "\nConnecting...";
            }
            tbTurnOn.Text = "";
            showDisconnectButtonTimer.Start();

            FillSelectedServer();
        }

        public void StartAnimation()
        {
            const double actualWidth = 385;

            double textGraphicalWidth = new FormattedText(tbString1.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface(tbString1.FontFamily.Source), tbString1.FontSize, tbString1.Foreground).WidthIncludingTrailingWhitespace;
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0;
            animation.To = -(textGraphicalWidth / 2);
            animation.RepeatBehavior = RepeatBehavior.Forever;
            animation.Duration = new Duration(TimeSpan.FromSeconds(20));
            tbString1.BeginAnimation(Canvas.LeftProperty, animation);
            tbString3.BeginAnimation(Canvas.LeftProperty, animation);
            tbString5.BeginAnimation(Canvas.LeftProperty, animation);
            tbString7.BeginAnimation(Canvas.LeftProperty, animation);

            textGraphicalWidth = new FormattedText(tbString2.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface(tbString2.FontFamily.Source), tbString2.FontSize, tbString2.Foreground).WidthIncludingTrailingWhitespace;
            
            animation = new DoubleAnimation();
            animation.From = actualWidth - textGraphicalWidth;
            animation.To = actualWidth - (textGraphicalWidth / 2);
            animation.RepeatBehavior = RepeatBehavior.Forever;
            animation.Duration = new Duration(TimeSpan.FromSeconds(20));
            tbString2.BeginAnimation(Canvas.LeftProperty, animation);
            tbString4.BeginAnimation(Canvas.LeftProperty, animation);
            tbString6.BeginAnimation(Canvas.LeftProperty, animation);

            //textGraphicalWidth = new FormattedText(tbString3.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface(tbString3.FontFamily.Source), tbString3.FontSize, tbString3.Foreground).WidthIncludingTrailingWhitespace;
            //tbString3.Text += tbString3.Text;
            //animation = new DoubleAnimation();
            //animation.From = 0;
            //animation.To = -textGraphicalWidth;
            //animation.RepeatBehavior = RepeatBehavior.Forever;
            //animation.Duration = new Duration(TimeSpan.FromSeconds(24));
            //tbString3.BeginAnimation(Canvas.LeftProperty, animation);

            //textGraphicalWidth = new FormattedText(tbString4.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface(tbString4.FontFamily.Source), tbString4.FontSize, tbString4.Foreground).WidthIncludingTrailingWhitespace;
            //tbString4.Text += tbString4.Text;
            //animation = new DoubleAnimation();
            //animation.From = actualWidth - (2 * textGraphicalWidth);
            //animation.To = actualWidth - textGraphicalWidth;
            //animation.RepeatBehavior = RepeatBehavior.Forever;
            //animation.Duration = new Duration(TimeSpan.FromSeconds(24));
            //tbString4.BeginAnimation(Canvas.LeftProperty, animation);

            //textGraphicalWidth = new FormattedText(tbString5.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface(tbString5.FontFamily.Source), tbString5.FontSize, tbString5.Foreground).WidthIncludingTrailingWhitespace;
            //tbString5.Text += tbString5.Text;
            //animation = new DoubleAnimation();
            //animation.From = 0;
            //animation.To = -textGraphicalWidth;
            //animation.RepeatBehavior = RepeatBehavior.Forever;
            //animation.Duration = new Duration(TimeSpan.FromSeconds(12));
            //tbString5.BeginAnimation(Canvas.LeftProperty, animation);

            //textGraphicalWidth = new FormattedText(tbString6.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface(tbString6.FontFamily.Source), tbString6.FontSize, tbString6.Foreground).WidthIncludingTrailingWhitespace;
            //tbString6.Text += tbString6.Text;
            //animation = new DoubleAnimation();
            //animation.From = actualWidth - (2 * textGraphicalWidth);
            //animation.To = actualWidth - textGraphicalWidth;
            //animation.RepeatBehavior = RepeatBehavior.Forever;
            //animation.Duration = new Duration(TimeSpan.FromSeconds(12));
            //tbString6.BeginAnimation(Canvas.LeftProperty, animation);

            //textGraphicalWidth = new FormattedText(tbString7.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface(tbString7.FontFamily.Source), tbString7.FontSize, tbString7.Foreground).WidthIncludingTrailingWhitespace;
            //tbString7.Text += tbString7.Text;
            //animation = new DoubleAnimation();
            //animation.From = 0;
            //animation.To = -textGraphicalWidth;
            //animation.RepeatBehavior = RepeatBehavior.Forever;
            //animation.Duration = new Duration(TimeSpan.FromSeconds(20));
            //tbString7.BeginAnimation(Canvas.LeftProperty, animation);
        }

        public void StopAnimation()
        {
            tbString1.BeginAnimation(Canvas.LeftProperty, null);
            tbString2.BeginAnimation(Canvas.LeftProperty, null);
            tbString3.BeginAnimation(Canvas.LeftProperty, null);
            tbString4.BeginAnimation(Canvas.LeftProperty, null);
            tbString5.BeginAnimation(Canvas.LeftProperty, null);
            tbString6.BeginAnimation(Canvas.LeftProperty, null);
            tbString7.BeginAnimation(Canvas.LeftProperty, null);
        }

        public void ShowProtectionOff()
        {
            protectionOn = false;

            StopAnimation();

            tbString1.Text = "| EMAILS | PASSWORDS | CHATS | PHOTOS | BANK ACCOUNTS | FACEBOOK | WEBSITES | PRIVATE DATA | SECRETS | DOCUMENTS | SMS | LOCATION | CREDIT CARDS ";
            tbString2.Text = "| SECRETS | DOCUMENTS | EMAILS | LOCATION | PASSWORDS | SMS | CHATS | CREDIT CARDS | PHOTOS | BANK ACCOUNTS | FACEBOOK | WEBSITES | PRIVATE DATA ";
            tbString3.Text = "| CREDIT CARDS | LOCATION | WEBSITES | PRIVATE DATA | SECRETS | DOCUMENTS | EMAILS | SMS | PASSWORDS | CHATS | PHOTOS | BANK ACCOUNTS | FACEBOOK ";
            tbString4.Text = "| LOCATION | SMS | BANK ACCOUNTS | WEBSITES | PRIVATE DATA | FACEBOOK | SECRETS | DOCUMENTS | EMAILS | CREDIT CARDS | PASSWORDS | CHATS | PHOTOS ";
            tbString5.Text = "| FACEBOOK | EMAILS | PASSWORDS | LOCATION | CHATS | PHOTOS | WEBSITES | CREDIT CARDS | PRIVATE DATA | SECRETS | DOCUMENTS | BANK ACCOUNTS | SMS ";
            tbString6.Text = "| SMS | FACEBOOK | WEBSITES | PRIVATE DATA | CREDIT CARDS | SECRETS | EMAILS | PASSWORDS | DOCUMENTS | LOCATION | PHOTOS | BANK ACCOUNTS | CHATS ";
            tbString7.Text = "| CHATS | PHOTOS | EMAILS | PASSWORDS | BANK ACCOUNTS | LOCATION | CREDIT CARDS | FACEBOOK | WEBSITES | PRIVATE DATA | SMS | SECRETS | DOCUMENTS ";

            tbString1.Text += tbString1.Text;
            tbString2.Text += tbString2.Text;
            tbString3.Text += tbString3.Text;
            tbString4.Text += tbString4.Text;
            tbString5.Text += tbString5.Text;
            tbString6.Text += tbString6.Text;
            tbString7.Text += tbString7.Text;    

            tbProtection.Text = "OFF";
            tbProtection.Foreground = new SolidColorBrush(Color.FromRgb(0xDD, 0, 0));

            tbStatus.Text = "To encrypt network activity and Wi-Fi hotspots, \nchange location and unblock sites -";
            tbTurnOn.Text = "turn on";

            imgShield.Visibility = Visibility.Collapsed;
            pathBrokenShield.Visibility = Visibility.Visible;

            chbSwitchProtection.IsChecked = false;

            tbSwitchProtection.Text = "Switch to enable protection";

            StartAnimation();
        }

        public void ShowProtectionOn()
        {
            protectionOn = true;

            StopAnimation();

            tbString1.Text = "1011327351216658976961895854109161375482153697372873064836573635724079182911187758461559881048658869939327764622294113695896133918495775169607998";
            tbString2.Text = "2931666081121910884051996741959743487181863681458661906299797270207395328671744962635378713265705430872520353901448587580781113595546755184495242";
            tbString3.Text = "4625612489495375699644228956140746753074364854609982789442712704102783001346218359262233611288828705323942593326259183338742286522189330385138699";
            tbString4.Text = "6154299360244377253017232057836938345207740695245976179496908782686053683280101298665856965039088085424547694106983218333355454223818119881705126";
            tbString5.Text = "4938525099354955695676881116436092197305208587392221795058284535726563925123862985589680440229685368514960768797844591422032840727854705008657500";
            tbString6.Text = "5212990473323618204416328566541934886002692189355716419817629665178358273914256266743402193351486760812441503893639251234424656544313136710784877";
            tbString7.Text = "7345551085192048721386810362681462902947741663693296092628513989058505870059923632207779914716712686476550895813787593264858518645571793013096874";

            tbString1.Text += tbString1.Text;
            tbString2.Text += tbString2.Text;
            tbString3.Text += tbString3.Text;
            tbString4.Text += tbString4.Text;
            tbString5.Text += tbString5.Text;
            tbString6.Text += tbString6.Text;
            tbString7.Text += tbString7.Text;

            tbProtection.Text = "ON";
            tbProtection.Foreground = new SolidColorBrush(Color.FromRgb(0x51, 0xAF, 0x2F));

            pathBrokenShield.Visibility = Visibility.Collapsed;
            imgShield.Visibility = Visibility.Visible;

            tbStatus.Text = "Network activity and Wi-Fi hotspots encrypted,\nreal location changed and sites unblocked";
            tbTurnOn.Text = "";

            chbSwitchProtection.IsChecked = true;

            tbSwitchProtection.Text = "Switch to disable protection";

            StartAnimation();
        }

        private void chbSwitchProtection_Click(object sender, RoutedEventArgs e)
        {
            if (selectedServer == null)
            {
                if (!initializing)
                {
                    tbCountry.Foreground = new SolidColorBrush(Colors.Red);
                }
                chbSwitchProtection.IsChecked = false;
                return;
            }
            if (!protectionOn && selectedServer.ConnectStatus == InternalStatus.Disconnected)
            {
                BeginConnect(selectedServer);
                chbSwitchProtection.IsChecked = false;
            }
            else
            {
                SyncConnector.Instance.BeginDisconnect(true);
                chbSwitchProtection.IsChecked = true;
            }
        }

        private void ChangeCountry_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SetPage(MainWindowPage.SelectList, true);
        }

        private void OnServerChanged(object sender, ServerEntryEventArgs args)
        {
            if (selectedServer != args.Entry)
            {
                if (selectedServer != null
                    && selectedServer.ConnectStatus != InternalStatus.Disconnected)
                {
                    BeginChangeServer(selectedServer, args.Entry);
                }
                else
                {
                    BeginConnect(args.Entry);
                }

                selectedServer = args.Entry;
                FillSelectedServer();
                GoogleAnalyticsApi.TrackEvent("country", "win", args.Entry.CountryCode);
            }
            else
            {
                if (selectedServer != null && selectedServer.ConnectStatus == InternalStatus.Disconnected)
                {
                    BeginConnect(selectedServer);
                    GoogleAnalyticsApi.TrackEvent("country", "win", selectedServer.CountryCode);
                }
            }
        }

        public void FillSelectedServer()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new SimpleHandler(FillSelectedServer), DispatcherPriority.Normal);
                return;
            }

            try
            {
                cbNode.Items.Clear();

                if (selectedServer == null)
                {
                    imgCountryFlag.Visibility = Visibility.Hidden;
                    tbCountry.Text = "Click to choose location";
                    return;
                }

                Uri uri = new Uri("pack://application:,,,/Privatix.Resources;component/small_flags/" + selectedServer.CountryCode + ".png");
                try
                {
                    imgCountryFlag.Source = new BitmapImage(uri);
                }
                catch
                {
                    imgCountryFlag.Source = new BitmapImage();
                }
                imgCountryFlag.Visibility = Visibility.Visible;
                tbCountry.Text = selectedServer.DisplayName;
                tbCountry.Foreground = new SolidColorBrush(Colors.Black);

                foreach (var node in selectedServer.Hosts)
                {
                    cbNode.Items.Add(node.Domen);
                }
                cbNode.SelectedIndex = selectedServer.CurrentHostIndex;
                var currentProto = SyncConnector.Instance.CurrentProtocol;
                cbProtocol.SelectedItem = currentProto == VpnProtocol.IPsec ? cbiProtoIpsec : currentProto == VpnProtocol.OpenVpnUDP ? cbiProtoOpenUdp : cbiProtoOpenTcp;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        private void ShowGuardDialog(InternalServerEntry entry)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new VoidInternalServerEntryHandler(ShowGuardDialog),
                                  DispatcherPriority.Normal,
                                  new object[] {entry});
                return;
            }


            Cursor prevCursor = Mouse.OverrideCursor;

            try
            {
                while (true)
                {
                    if (entry != null && entry.ConnectStatus != InternalStatus.Disconnected)
                    {
                        Logger.Log.Warn("Selected server not disconnected");
                        return;
                    }

                    GuardWindow guard = new GuardWindow();
                    guard.Owner = Window.GetWindow(this);
                    var result = guard.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        if (entry != null && App.Site.Servers.Contains(entry)
                            && (entry.IsFree || !App.Site.IsFree))
                        {
                            if (entry.ConnectStatus == InternalStatus.Disconnected
                                || entry.ConnectStatus == InternalStatus.Error)
                            {
                                GlobalEvents.Connecting(this, entry);
                                BeginConnect(entry);

                                Guard.Instance.SetReconnected();
                            }

                            break;
                        }
                        else
                        {
                            MessageBox.Show("The selected server is no longer available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Logger.Log.Error("Error! The selected server is no longer available.");
                            continue;
                        }
                    }
                    else
                    {
                        Guard.Instance.DisableGuard();
                        break;
                    }
                }
            }
            finally
            {
                Mouse.OverrideCursor = prevCursor;
            }
        }

        private void BeginConnect(InternalServerEntry entry)
        {
            App.RegisterUserAction();

            LockNotification lockNotification = App.Site.GetLock();
            if (App.Site.IsFree && lockNotification != null && lockNotification.TryLock())
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    Dispatcher.Invoke(new VoidINotificationHandler(ShowLock),
                        DispatcherPriority.Normal,
                        new object[] { lockNotification });

                    SyncConnector.Instance.BeginConnect(entry, GetNodeSelectedIndex());
                });
            }
            else
            {
                SyncConnector.Instance.BeginConnect(entry, GetNodeSelectedIndex());
            }            
        }

        private void BeginChangeServer(InternalServerEntry oldServer, InternalServerEntry newServer)
        {
            App.RegisterUserAction();

            LockNotification lockNotification = App.Site.GetLock();
            if (App.Site.IsFree && lockNotification != null && lockNotification.TryLock())
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    SyncConnector.Instance.BeginDisconnect(true);
                    Dispatcher.Invoke(new VoidINotificationHandler(ShowLock),
                        DispatcherPriority.Normal,
                        new object[] { lockNotification });

                    SyncConnector.Instance.BeginConnect(newServer, GetNodeSelectedIndex());
                });
            }
            else
            {
                SyncConnector.Instance.BeginChangeServer(oldServer, newServer);
            }
        }

        private void ShowLock(INotification notification)
        {
            LockNotification lockNotification = notification as LockNotification;
            if (lockNotification == null)
            {
                return;
            }

            GoogleAnalyticsApi.TrackEvent("waiting", "win");

            LockWindow lockWindow = new LockWindow();
            lockWindow.Start(lockNotification.Ttl);
            lockWindow.ShowDialog();
        }

        private void ShowDisconnectButton(object sender, EventArgs e)
        {
            if (selectedServer != null && selectedServer.ConnectStatus == InternalStatus.Connecting)
            {
                chbSwitchProtection.IsChecked = true;
                chbSwitchProtection.IsEnabled = true;
                showDisconnectButtonTimer.Stop();
            }
        }

        private void cbNode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!cbNode.IsDropDownOpen)
            {
                return;
            }

            if (selectedServer != null)
            {
                selectedServer.SetCurrentHostIndex(cbNode.SelectedIndex);

                if (selectedServer.ConnectStatus != InternalStatus.Disconnected)
                {
                    BeginChangeServer(selectedServer, selectedServer);
                }
            }           
        }

        private void cbProtocol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!cbProtocol.IsDropDownOpen)
            {
                return;
            }

            VpnProtocol protocol = cbProtocol.SelectedItem == cbiProtoIpsec ? VpnProtocol.IPsec : cbProtocol.SelectedItem == cbiProtoOpenUdp ? VpnProtocol.OpenVpnUDP : VpnProtocol.OpenVpnTCP;
            SyncConnector.Instance.ChangeCurrentProtocol(protocol);

            if (selectedServer != null && selectedServer.ConnectStatus != InternalStatus.Disconnected)
            {
                BeginChangeServer(selectedServer, selectedServer);
            }
        }

        private int GetNodeSelectedIndex()
        {
            if (!Dispatcher.CheckAccess())
            {
                return (int)Dispatcher.Invoke(new IntHandler(GetNodeSelectedIndex),
                                  DispatcherPriority.Normal,
                                  null);
            }


            return cbNode.SelectedIndex >= 0 ? cbNode.SelectedIndex : 0;
        }
    }
}
