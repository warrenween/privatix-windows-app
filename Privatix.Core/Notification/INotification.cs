using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.Notification
{
    public interface INotification
    {
        NotificationType NotifyType { get; }
        int Ttl { get; }
        int Period { get; }
        string Target { get; }
        string Text { get; }
        string Url { get; }
        string Link { get; }

        bool Compare(Privatix.Core.Site.Notification notify);
        void Close();
    }


    public enum NotificationType
    {
        Banner,
        Page,
        Lock,
        Disconnect
    }
}
