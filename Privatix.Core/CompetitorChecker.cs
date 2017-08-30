using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Privatix.Core
{
    public class CompetitorChecker : MarshalByRefObject
    {
        private static CompetitorChecker _instance = null;
        public static CompetitorChecker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CompetitorChecker();
                }

                return _instance;
            }
        }

        public ICollection<string> Check(IEnumerable<string> competitors)
        {
            HashSet<string> finded = new HashSet<string>();
            List<string> allPrograms = new List<string>();

            if (competitors == null || competitors.Count<string>() == 0)
            {
                return finded;
            }

            try
            {
                object line;
                string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var key = baseKey.OpenSubKey(registry_key))
                    {
                        foreach (string subkey_name in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subkey_name))
                            {
                                line = subKey.GetValue("DisplayName");
                                if (line != null && line is string)
                                {
                                    allPrograms.Add(line as string);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                object line;
                string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var key = baseKey.OpenSubKey(registry_key))
                    {
                        foreach (string subkey_name in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subkey_name))
                            {
                                line = subKey.GetValue("DisplayName");
                                if (line != null && line is string)
                                {
                                    allPrograms.Add(line as string);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                object line;
                string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    using (var key = baseKey.OpenSubKey(registry_key))
                    {
                        foreach (string subkey_name in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subkey_name))
                            {
                                line = subKey.GetValue("DisplayName");
                                if (line != null && line is string)
                                {
                                    allPrograms.Add(line as string);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                object line;
                string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
                {
                    using (var key = baseKey.OpenSubKey(registry_key))
                    {
                        foreach (string subkey_name in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subkey_name))
                            {
                                line = subKey.GetValue("DisplayName");
                                if (line != null && line is string)
                                {
                                    allPrograms.Add(line as string);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            foreach (string comp in competitors)
            {
                string lowerComp = comp.ToLower();
                var compFindedList = allPrograms.Where(s => s.ToLower().Contains(lowerComp));
                if (compFindedList != null && compFindedList.Count<string>() > 0)
                {

                    finded.Add(comp);
                }
            }

            return finded;
        }

        public override object InitializeLifetimeService()
        {
            return (null);
        }
    }
}
