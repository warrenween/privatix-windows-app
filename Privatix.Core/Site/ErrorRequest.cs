using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Privatix.Core.Site
{
    class ErrorRequest
    {
        public string type { get; set; }
        public int datetime { get; set; }
        public string subscription_uuid { get; set; }
        public string error { get; set; }
        public string error_trace { get; set; }
        public string source_country { get; set; }
        public string connection_node { get; set; }
    }
}
