using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using Privatix.Core;
using Privatix.Core.Delegates;
using Privatix.Core.OpenVpn;


namespace InstallerExtensions
{
    public static class DriverInstallerAction
    {

        private static Session currentSession;

        [CustomAction]
        public static ActionResult InstallDrivers(Session session)
        {
            currentSession = session;

            try
            {
                string openvpnDir = session.CustomActionData["OPENVPNDIR"];
                string driverDir = session.CustomActionData["OPENVPNDRIVERDIR"];
                Registry.SetValue(Config.RegRootMachine, "OpenVpnDir", openvpnDir, RegistryValueKind.String);
                Registry.SetValue(Config.RegRootMachine, "OpenVpnDriversDir", driverDir);

                session.Log("Start check drivers." );
                if (DriverInstaller.IsInterfaceExists())
                {
                    session.Log("OpenVPN drivers already installed.");
                    return ActionResult.SkipRemainingActions;
                }

                session.Log("Start drivers install.");
                DriverInstaller.OnInstallTextMessage += DriverInstaller_OnInstallTextMessage;
                if (DriverInstaller.Install(driverDir))
                {
                    return ActionResult.Success;
                }
                else
                {
                    session.Log("Drivers install failed.");
                    return ActionResult.Failure;
                }
            }
            catch (Exception ex)
            {
                session.Log("Error: " + ex.Message);
                session.Log(ex.StackTrace);
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult UninstallDrivers(Session session)
        {
            currentSession = session;

            OpenVpnHelper helper = new OpenVpnHelper();
            helper.CloseAllProcesses();

            DriverInstaller.OnInstallTextMessage += DriverInstaller_OnInstallTextMessage;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(Config.RegRoot, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("OpenVpnDir");
                        key.DeleteValue("OpenVpnDriversDir");
                    }
                }
            }
            catch 
            {
            }

            try
            {
                session.Log("Start check drivers.");
                if (!DriverInstaller.IsInterfaceExists())
                {
                    session.Log("OpenVPN interface not found.");
                    return ActionResult.SkipRemainingActions;
                }

                session.Log("Start drivers uninstall.");
                string driverDir = session.CustomActionData["OPENVPNDRIVERDIR"];
                session.Log("driverDir = " + driverDir);
                if (DriverInstaller.Uninstall(driverDir))
                {
                    return ActionResult.Success;
                }
                else
                {
                    session.Log("Drivers uninstall failed.");
                    return ActionResult.Failure;
                }
            }
            catch (Exception ex)
            {
                session.Log("Error: " + ex.Message);
                session.Log(ex.StackTrace);
                return ActionResult.Failure;
            }
        }

        private static void DriverInstaller_OnInstallTextMessage(object sender, TextEventArgs args)
        {
            currentSession.Log(args.Text);
        }
    }
}
