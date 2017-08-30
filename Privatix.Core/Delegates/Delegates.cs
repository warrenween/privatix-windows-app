using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Privatix.Core.Notification;

namespace Privatix.Core.Delegates
{
    public delegate void IntEventHandler(object sender, IntEventArgs args);
    public delegate void TextEventHandler(object sender, TextEventArgs args);
    public delegate void ServerEntryEventHandler(object sender, ServerEntryEventArgs args);
    public delegate void NotificationEventHandler(object sender, NotificationEventArgs args);
    public delegate void MessageEventHandler(object sender, MessageEventArgs args);    

    public delegate void SimpleHandler();
    public delegate int IntHandler();
    public delegate void VoidIntHandler(int param);
    public delegate void VoidStringHandler(string text);    
    public delegate void VoidINotificationHandler(INotification notification);
    public delegate void VoidInternalServerEntryHandler(InternalServerEntry entry);
}
