using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core
{
    public interface IConnector
    {
        bool DoConnect();
        void DoDisconnect();
        void Close();
    }
}
