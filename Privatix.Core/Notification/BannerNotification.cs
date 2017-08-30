using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Privatix.Core.Delegates;


namespace Privatix.Core.Notification
{
    public class BannerNotification : INotification
    {
        #region Fields

        private int period;
        private int ttl;
        private string target;
        private string text;
        private string url;
        private string link;
        private Timer timer;
        public object BannerImage = null;
        public byte[] ImageBytes = null;

        #endregion

        #region Properties

        public NotificationType NotifyType
        {
            get { return NotificationType.Banner; }
        }

        public int Period
        {
            get { return period; }
        }

        public int Ttl
        {
            get { return ttl; }
        }

        public string Target
        {
            get { return target; }
        }

        public string Text
        {
            get { return text; }
        }

        public string Url
        {
            get { return url; }
        }

        public string Link
        {
            get { return link; }
        }

        #endregion

        #region Ctors

        public BannerNotification(int _period, int _ttl, string _target, string _text, string _url, string _link)
        {
            period = _period;
            ttl = _ttl;
            target = _target;
            text = _text;
            url = _url;
            link = _link;

            if (!string.IsNullOrEmpty(url))
            {
                LoadBanner(1);
            }
            else
            {
                if (string.Equals(target, "state:disconnected", StringComparison.OrdinalIgnoreCase))
                {
                    GlobalEvents.OnConnected += GlobalEvents_OnConnected;
                }
                else if (string.Equals(target, "state:free_connected", StringComparison.OrdinalIgnoreCase))
                {
                    GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;
                }
            }

            timer = new Timer(TimerCallback);
            timer.Change(0, Timeout.Infinite);
        }

        #endregion

        #region Methods

        public void Close()
        {
            if (timer != null)
            {
                try
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    timer.Dispose();
                }
                catch (Exception)
                {
                }
            }

            if (string.IsNullOrEmpty(url))
            {
                GlobalEvents.ShowTrayNotify(this, null);
            }
        }

        public bool Compare(Privatix.Core.Site.Notification notify)
        {
            if (string.IsNullOrEmpty(notify.type))
                return false;

            if (notify.type.ToLower() != "banner")
                return false;

            if (notify.period != period)
                return false;

            if (notify.ttl != ttl)
                return false;

            if (string.IsNullOrEmpty(notify.target))
            {
                if (!string.IsNullOrEmpty(target))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(target))
                {
                    return false;
                }

                if (!string.Equals(notify.target, target, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(notify.text))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(text))
                {
                    return false;
                }

                if (!string.Equals(notify.text, text, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(notify.url))
            {
                if (!string.IsNullOrEmpty(url))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(url))
                {
                    return false;
                }

                if (!string.Equals(notify.url, url, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(notify.link))
            {
                if (!string.IsNullOrEmpty(link))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(link))
                {
                    return false;
                }


                if (!string.Equals(notify.link, link, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;            
        }

        public void TimerCallback(object state)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (ImageBytes == null)
                {
                    LoadBanner(10);
                }
                GlobalEvents.ShowBanner(this, this);
            }
            else
            {
                GlobalEvents.ShowTrayNotify(this, this);
            }

            try
            {
                timer.Change(period * 1000, Timeout.Infinite);
            }
            catch (Exception) { }
        }

        private void LoadBanner(int attempts)
        {
            while (attempts > 0)
            {
                try
                {
                    ImageBytes = new WebClient().DownloadData(url);
                    break;
                }
                catch
                {
                    ImageBytes = null;
                    Thread.Sleep(2000);
                    attempts--;
                }
            }
        }

        private void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            GlobalEvents.ShowTrayNotify(this, null);
        }

        private void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            GlobalEvents.ShowTrayNotify(this, null);
        }

        #endregion
    }
}
