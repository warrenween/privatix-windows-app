using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.Site
{
    class OAuthRequest
    {
        public string email { get; set; }
        public string token { get; set; }
        public string provider { get; set; }
    }
}
