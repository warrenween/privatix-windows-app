using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Privatix.Version
{
    class Program
    {
        static void Main(string[] args)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
#if _DEV_API_
            string target = "dev";
#elif _STAGE_API_
            string target = "stage";
#elif _VERSION_BUILD_
            string target = "version";
#else
            string target = "production";
#endif

            Console.WriteLine("##teamcity[buildNumber '{0}']", version.ToString(4));
            Console.WriteLine("##teamcity[setParameter name='TARGET' value='{0}']", target);
            Console.WriteLine("##teamcity[setParameter name='VERSION' value='{0}']", version.ToString(3));
        }
    }
}
