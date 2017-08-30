using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Privatix.Core.Site
{
    public class Subscription
    {
        public Quotes quotes { get; set; }
        public string subscription_id { get; set; }
        public long created_at { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public string plan { get; set; }
        public bool active { get; set; }
        public IList<Node> nodes { get; set; }
    }

    public class Quotes
    {
        public long expires_at { get; set; }
        public Bandwidth bandwidth { get; set; }
        public Time time { get; set; }
        public Sessions sessions { get; set; }
    }

    public class Bandwidth
    {
        public int used { get; set; }
        public int limit { get; set; }
        public int available { get; set; }
    }

    public class Time
    {
        public int used { get; set; }
        public int limit { get; set; }
        public int available { get; set; }
        public int pause { get; set; }
    }

    public class Sessions
    {
        public int used { get; set; }
        public int limit { get; set; }
        public int available { get; set; }
    }

    public class Node
    {
        public string city { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
        public string display_name { get; set; }
        public int priority { get; set; }
        public bool free {get; set; }
        public IList<string> mode { get; set; }
        public IList<Host> hosts { get; set; }
    }

    public class Host
    {
        public string host { get; set; }
        public int port { get; set; }
        public IList<string> mode { get; set; }
        public string timezone { get; set; }
    }

}
