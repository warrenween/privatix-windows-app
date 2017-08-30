using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Privatix.Core.Site
{
    class ActivationRequest
    {
        public Device device { get; set; }
        public Software software { get; set; }
    }

    class Device
    {
        public string name { get; set; }
        public string type { get; set; }
        public string model { get; set; }
        public string device_id { get; set; }
        public OperationSystem os { get; set; }
    }

    class Software
    {
        public string type { get; set; }
        public string source { get; set; }
        public string version { get; set; }
    }

    class OperationSystem
    {
        public string name { get; set; }
        public string version { get; set; }
        public string family { get; set; }
    }
}
