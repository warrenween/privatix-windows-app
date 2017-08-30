using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Privatix.Core.Notification;


namespace Privatix.Core.Delegates
{
    public class IntEventArgs : EventArgs
    {
        public int Param;

        public IntEventArgs(int param)
        {
            Param = param;
        }
    }

    [Serializable]
    public class TextEventArgs : EventArgs
    {
        public string Text;

        public TextEventArgs(string text)
        {
            Text = text;
        }
    }

    public class ServerEntryEventArgs : EventArgs
    {
        public InternalServerEntry Entry;

        public ServerEntryEventArgs(InternalServerEntry entry)
        {
            Entry = entry;
        }
    }

    public class NotificationEventArgs : EventArgs
    {
        public INotification Notify;

        public NotificationEventArgs(INotification notify)
        {
            Notify = notify;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Text;
        public string Caption;

        public MessageEventArgs(string text, string caption)
        {
            Text = text;
            Caption = caption;
        }
    }  
}
