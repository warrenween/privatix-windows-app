using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Privatix.Core
{
    public class PrivatixWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 20 * 1000;
            return w;
        }
    }
}
