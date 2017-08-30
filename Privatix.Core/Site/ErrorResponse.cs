using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Privatix.Core.Site
{
    class ErrorResponse
    {
        public string status { get; set; }
        public string error { get; set; }
        public int server_time { get; set; }
        public string message { get; set; }
        public int error_code { get; set; }
    }
}
