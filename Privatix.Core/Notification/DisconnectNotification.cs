using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Privatix.Core.Delegates;


namespace Privatix.Core.Notification
{
    public class DisconnectNotification : INotification
    {
        #region Fields

        private int ttl;

        #endregion

        #region Properties

        public NotificationType NotifyType
        {
            get { return NotificationType.Disconnect; }
        }

        public int Period
        {
            get { return 0; }
        }

        public int Ttl
        {
            get { return ttl; }
        }

        public string Target
        {
            get { return ""; }
        }

        public string Text
        {
            get { return ""; }
        }

        public string Url
        {
            get { return ""; }
        }

        public string Link
        {
            get { return ""; }
        }

        #endregion

        #region Ctors

        public DisconnectNotification(int _ttl)
        {
            ttl = _ttl;

            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
        }

        void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            GlobalEvents.DisconnectTimerUpdated(this, -1);
        }

        void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            if (SiteConnector.Instance.IsFree)
            {
                GlobalEvents.DisconnectTimerUpdated(this, ttl);
            }
        }

        #endregion

        #region Methods

        public void Close()
        {
            //TODO:
        }

        public bool Compare(Privatix.Core.Site.Notification notify)
        {
            if (string.IsNullOrEmpty(notify.type))
                return false;

            if (notify.type.ToLower() != "disconnect")
                return false;

            if (notify.ttl != ttl)
                return false;

            return true;            
        }

        #endregion
    }
}
