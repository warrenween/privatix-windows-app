using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.Site
{
    public class Notification
    {
        public string type { get; set; }
        public string format { get; set; }
        public int ttl { get; set; }
        public int period { get; set; }
        public string target { get; set; }
        public string url { get; set; }
        public string text { get; set; }
        public string link { get; set; }
    }

    class NotificationsResponse
    {
        public string status { get; set; }
        public IList<Notification> notifications { get; set; }
    }
}
