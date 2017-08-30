using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;


namespace Privatix.Core
{
    public class ServiceWorker : MarshalByRefObject
    {
        private static ServiceWorker _instance = null;
        public static ServiceWorker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceWorker();
                }

                return _instance;
            }
        }

        public bool WriteRegValue(string valueName, string value)
        {
            try
            {
                Registry.SetValue(Config.RegRootMachine, valueName, value, RegistryValueKind.String);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }
        }

        public bool DeleteRegValue(string valueName)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(Config.RegRoot, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(valueName);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }
        }

        public override object InitializeLifetimeService()
        {
            return (null);
        }
    }
}
