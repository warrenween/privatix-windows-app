using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.Notification
{
    public class LockNotification : INotification
    {

        #region Fields

        private int period;
        private int ttl;
        private string target;
        private string text;
        private string url;
        private string link;
        private int tryCount;

        #endregion

        #region Properties

        public NotificationType NotifyType
        {
            get { return NotificationType.Lock; }
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

        public LockNotification(int _period, int _ttl, string _target, string _text, string _url, String _link)
        {
            period = _period;
            ttl = _ttl;
            target = _target;
            text = _text;
            url = _url;
            link = _link;
            tryCount = 0;
        }

        #endregion

        #region Methods

        public void Close()
        {
            tryCount = 0;
        }

        public bool Compare(Privatix.Core.Site.Notification notify)
        {
            if (string.IsNullOrEmpty(notify.type))
                return false;

            if (notify.type.ToLower() != "lock")
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

            return true;            
        }

        public bool TryLock()
        {
            if (ttl <= 0)
            {
                return false;
            }

            tryCount++;

            if (tryCount >= period)
            {
                tryCount = 0;
                return true;
            }

            return false;
        }

        #endregion

    }
}
