using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.Notification
{
    public class PageNotification : INotification
    {
        #region Fields

        private int period;
        private int ttl;
        private string target;
        private string text;
        private string url;
        private string link;

        #endregion

        #region Properties

        public NotificationType NotifyType
        {
            get { return NotificationType.Page; }
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

        public PageNotification(int _period, int _ttl, string _target, string _text, string _url, string _link)
        {
            period = _period;
            ttl = _ttl;
            target = _target;
            text = _text;
            url = _url;
            link = _link;
        }

        #endregion

        #region Methods

        public void Close()
        {

        }

        public bool Compare(Privatix.Core.Site.Notification notify)
        {
            if (string.IsNullOrEmpty(notify.type))
                return false;

            if (notify.type.ToLower() != "page")
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

        #endregion
    }
}
