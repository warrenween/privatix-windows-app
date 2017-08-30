using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.OpenVpn
{
    public abstract class IOpenVpnReceiver : MarshalByRefObject
    {
        public abstract void OnProcessExited(object sender, EventArgs e);
        public abstract void OnErrorDataReceived(object sender, Delegates.TextEventArgs args);
        public abstract void OnOutputDataReceived(object sender, Delegates.TextEventArgs args);

        
    }
}
