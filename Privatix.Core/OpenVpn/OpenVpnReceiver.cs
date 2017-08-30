using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Privatix.Core.Delegates;


namespace Privatix.Core.OpenVpn
{
    public class OpenVpnReceiver : IOpenVpnReceiver
    {
        public event TextEventHandler OutputDataReceived;
        public event TextEventHandler ErrorDataReceived;
        public event EventHandler ProcessExited;

        public override void OnProcessExited(object sender, EventArgs e)
        {
            if (ProcessExited != null)
            {
                ProcessExited(sender, e);
            }
        }

        public override void OnErrorDataReceived(object sender, Delegates.TextEventArgs args)
        {
            if (ErrorDataReceived != null)
            {
                ErrorDataReceived(sender, args);
            }
        }

        public override void OnOutputDataReceived(object sender, Delegates.TextEventArgs args)
        {
            if (OutputDataReceived != null)
            {
                OutputDataReceived(sender, args);
            }
        }

        public override object InitializeLifetimeService()
        {
            return (null);
        }
    }
}
