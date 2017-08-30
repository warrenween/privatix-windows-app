using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;
using Privatix.Core;
using Privatix.Core.Delegates;


namespace Privatix.Controls
{
    /// <summary>
    /// Логика взаимодействия для ServerList.xaml
    /// </summary>
    public partial class ServerList : UserControl
    {
        private InternalServerEntry lastSelectedServerEntry = null;


        public ServerList()
        {
            InitializeComponent();

            GlobalEvents.OnSubscriptionUpdated += OnSubscriptionChanged;
            FillServersList();
        }

        private void OnSubscriptionChanged(object sender, EventArgs args)
        {
            FillServersList();
        }

        public void FillServersList()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new SimpleHandler(FillServersList), DispatcherPriority.Normal);
                return;
            }

            try
            {
                lbServers.Items.Clear();

                foreach (InternalServerEntry entry in SiteConnector.Instance.Servers)
                {
                    ServerListItem serverListItem = new ServerListItem();
                    serverListItem.ServerEntry = entry;

                    if (App.Site.IsFree)
                    {
                        serverListItem.IsAvailable = entry.IsFree;
                    }

                    if (serverListItem.IsAvailable && lastSelectedServerEntry == entry && entry.ConnectStatus == InternalStatus.Connected)
                    {
                        serverListItem.IsChecked = true;
                    }

                    lbServers.Items.Add(serverListItem);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
            finally
            {

            }
        }

        private void lbServers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ServerListItem newSelectedItem = null;

            try
            {
                if (e.AddedItems.Count > 0)
                {
                    newSelectedItem = e.AddedItems[0] as ServerListItem;
                }

                if (newSelectedItem != null)
                {
                    if (newSelectedItem.IsAvailable)
                    {
                        lastSelectedServerEntry = newSelectedItem.ServerEntry;
                        GlobalEvents.ChangeServer(this, lastSelectedServerEntry);
                    }
                    else
                    {

                        Process.Start(Config.ServerListPremiumUrl);
                        GoogleAnalyticsApi.TrackEvent("premium", "win");
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
            finally
            {
                lbServers.SelectedItem = null;
                MainWindow.Instance.SetPage(MainWindowPage.Protection, false);
            }            
        }
    }
}
