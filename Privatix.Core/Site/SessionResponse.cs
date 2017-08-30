using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Privatix.Core.Site
{
    class SessionResponse
    {
        public string status { get; set; }
        public long server_time { get; set; }
        public string country { get; set; }
        public string original_country { get; set; }
        public bool is_authorized { get; set; }
        public string email { get; set; }
        public bool is_verified { get; set; }
        public bool connected { get; set; }        
        public string current_ip { get; set; }
        public Subscription subscription { get; set; }
        public IList<Notification> notifications { get; set; }
    }
}
