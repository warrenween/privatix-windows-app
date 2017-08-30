using System;
using Privatix.Core.Delegates;
using Privatix.Core.Notification;


namespace Privatix.Core
{
    public static class GlobalEvents
    {
        public static event EventHandler OnIpInfoUpdated;
        public static event EventHandler OnSubscriptionUpdated;
        public static event ServerEntryEventHandler OnConnecting;
        public static event ServerEntryEventHandler OnConnected;
        public static event ServerEntryEventHandler OnDisconnected;
        public static event ServerEntryEventHandler OnDisconnecting;
        public static event ServerEntryEventHandler OnChangeServer;
        public static event ServerEntryEventHandler OnEnded;
        public static event NotificationEventHandler OnShowBanner;
        public static event NotificationEventHandler OnShowTrayNotify;
        public static event NotificationEventHandler OnLock;
        public static event EventHandler OnSecureNetworkNotify;
        public static event MessageEventHandler OnShowMessage;
        public static event IntEventHandler OnDisconnectTimerUpdated;
        public static event EventHandler OnForceUpdate;


        public static void IpInfoUpdated(object sender)
        {
            if (OnIpInfoUpdated != null)
                OnIpInfoUpdated(sender, new EventArgs());
        }

        public static void SubscriptionUpdated(object sender)
        {
            if (OnSubscriptionUpdated != null)
                OnSubscriptionUpdated(sender, new EventArgs());
        }

        public static void Connecting(object sender, InternalServerEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.ConnectStatus == InternalStatus.Connecting)
            {
                return;
            }

            entry.ConnectStatus = InternalStatus.Connecting;

            if (OnConnecting != null)
            {
                OnConnecting(sender, new ServerEntryEventArgs(entry));
            }
        }

        public static void Disconnecting(object sender, InternalServerEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.ConnectStatus == InternalStatus.Disconnecting)
            {
                return;
            }

            entry.ConnectStatus = InternalStatus.Disconnecting;

            if (OnDisconnecting != null)
            {
                OnDisconnecting(sender, new ServerEntryEventArgs(entry));
            }
        }

        public static void Connected(object sender, InternalServerEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.ConnectStatus == InternalStatus.Connected)
            {
                return;
            }

            entry.ConnectStatus = InternalStatus.Connected;

            if (OnConnected != null)
            {
                OnConnected(sender, new ServerEntryEventArgs(entry));
            }
        }

        public static void Disconnected(object sender, InternalServerEntry entry)
        {
            //Logger.Log.Info(Environment.StackTrace);

            if (entry == null)
            {
                return;
            }

            if (entry.ConnectStatus == InternalStatus.Disconnected)
            {
                Logger.Log.Info("Already disconnected");
                return;
            }

            entry.ConnectStatus = InternalStatus.Disconnected;

            if (OnDisconnected != null)
            {
                OnDisconnected(sender, new ServerEntryEventArgs(entry));
            }

            Ended(sender, entry);
        }

        public static void Ended(object sender, InternalServerEntry entry)
        {
            if (OnEnded != null)
            {
                OnEnded(sender, new ServerEntryEventArgs(entry));
            }
        }

        public static void ChangeServer(object sender, InternalServerEntry entry)
        {
            if (OnChangeServer != null)
            {
                OnChangeServer(sender, new ServerEntryEventArgs(entry));
            }
        }

        public static void ShowBanner(object sender, INotification notify)
        {
            if (OnShowBanner != null)
            {
                OnShowBanner(sender, new NotificationEventArgs(notify));
            }
        }

        public static void ShowTrayNotify(object sender, INotification notify)
        {
            if (OnShowTrayNotify != null)
            {
                OnShowTrayNotify(sender, new NotificationEventArgs(notify));
            }
        }

        public static void Lock(object sender, INotification notify)
        {
            if (OnLock != null)
            {
                OnLock(sender, new NotificationEventArgs(notify));
            }
        }

        public static void SecureNetworkNotify(object sender)
        {
            if (OnSecureNetworkNotify != null)
            {
                OnSecureNetworkNotify(sender, new EventArgs());
            }
        }

        public static void ShowMessage(object sender, string text, string caption)
        {
            if (OnShowMessage != null)
            {
                OnShowMessage(sender, new MessageEventArgs(text, caption));
            }
        }

        public static void DisconnectTimerUpdated(object sender, int param)
        {
            if (OnDisconnectTimerUpdated != null)
            {
                OnDisconnectTimerUpdated(sender, new IntEventArgs(param));
            }
        }

        public static void ForceUpdate(object sender)
        {
            if (OnForceUpdate != null)
            {
                OnForceUpdate(sender, new EventArgs());
            }
        }
    }
}
