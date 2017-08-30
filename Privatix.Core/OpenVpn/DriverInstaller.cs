using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Privatix.Core.Delegates;


namespace Privatix.Core.OpenVpn
{
    public static class DriverInstaller
    {
        #region OnInstallTextMessage event
        
        public static event TextEventHandler OnInstallTextMessage;

        #endregion


        private static bool installResult = false;
        private static bool uninstallResult = false;
        private static bool installError = false;


        public static bool IsInterfaceExists()
        {
            Logger.Log.InfoFormat("Checking interface for HardwareId = \"{0}\" and Connection Name = \"{1}\"", Config.OpenVpnTapId, Config.OpenVpnTapName);

            bool result = false;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}")) //Network adapters
            {
                if (key == null)
                {
                    return false;
                }

                string[] adapterNames = key.GetSubKeyNames();
                foreach (var adapterName in adapterNames)
                {
                    using (RegistryKey adapterConnection = key.OpenSubKey(adapterName + "\\Connection", false))
                    {
                        if (adapterConnection == null)
                        {
                            continue;
                        }

                        string name = adapterConnection.GetValue("Name", "") as string;

                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        if (name.Equals(Config.OpenVpnTapName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            Logger.Log.InfoFormat("Found interface with Name = \"{0}\"", name);

                            string pnpId = adapterConnection.GetValue("PnpInstanceID") as string;
                            if (pnpId != null)
                            {
                                using (RegistryKey dev = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + pnpId, false))
                                {
                                    if (dev != null)
                                    {
                                        string[] hwid = dev.GetValue("HardwareID") as string[];
                                        if (hwid != null && hwid.Length > 0 && !string.IsNullOrEmpty(hwid[0])
                                            && hwid[0].Equals(Config.OpenVpnTapId.ToLower(), StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            Logger.Log.InfoFormat("Found interface with PnpInstanceId = \"{0}\"", pnpId);

                                            result = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                } 
            }

            return result;
        }

        private static void VerifyDriverFiles(string driverDir)
        {
            TextEvent("Verifying interface files...");

            if (!File.Exists(Path.Combine(driverDir,  "OemVista.inf")))
                throw new Exception("INF-file for TAP driver not found");

            if (!File.Exists(Path.Combine(driverDir, Config.OpenVpnTapId + ".sys")))
                throw new Exception("SYS-file for TAP driver not found");

            if (!File.Exists(Path.Combine(driverDir, Config.OpenVpnTapId + ".cat")))
                throw new Exception("CAT-file for TAP driver not found");

            if (!File.Exists(Path.Combine(driverDir, "tapinstall.exe")))
                throw new Exception("File tapinstall.exe not found");

            TextEvent("Verification complete OK");
        }

        public static bool Install(string driverDir)
        {
            installResult = false;
            installError = false;

            VerifyDriverFiles(driverDir);

            NetworkInterface[] interfacesBefore = NetworkInterface.GetAllNetworkInterfaces();

            string tapInstallFilename = Path.Combine(driverDir, "tapinstall.exe");
            string infFilename = Path.Combine(driverDir, "OemVista.inf");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = tapInstallFilename,
                Arguments = "install \"" + infFilename + "\" " + Config.OpenVpnTapId,
                WorkingDirectory = driverDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process tapInstall = new Process();
            tapInstall.StartInfo = psi;
            tapInstall.ErrorDataReceived += tapInstall_ErrorDataReceived;
            tapInstall.OutputDataReceived += tapInstall_OutputDataReceived;
            tapInstall.EnableRaisingEvents = true;
            tapInstall.Start();
            tapInstall.BeginOutputReadLine();
            tapInstall.BeginErrorReadLine();

            try
            {
                while (!tapInstall.WaitForExit(500))
                {
                    if (installResult)
                    {
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Log.Warn(ex.Message, ex);
            }

            if (!tapInstall.HasExited)
            {
                tapInstall.Kill();
            }

            int count = 5;
            while (!installResult && !installError && count > 0)
            {
                Thread.Sleep(1000);
                count--;
            }

            if (installResult)
            {
                NetworkInterface[] interfacesAfter = NetworkInterface.GetAllNetworkInterfaces();
                NetworkInterface newInterface = interfacesAfter.FirstOrDefault(i => interfacesBefore.All(j => j.Id != i.Id));
                if (newInterface == null)
                {
                    TextEvent("New interface was not created while driver install!", true);
                    return false;
                }

                bool found = false;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}"))
                {
                    if (key != null)
                    {
                        string[] adapterNames = key.GetSubKeyNames();
                        foreach (var adapterName in adapterNames)
                        {
                            using (RegistryKey adapterConnection = key.OpenSubKey(adapterName + "\\Connection", true))
                            {
                                if (adapterConnection == null)
                                {
                                    continue;
                                }

                                string name = adapterConnection.GetValue("Name", "") as string;
                                if (string.IsNullOrEmpty(name))
                                {
                                    continue;
                                }

                                if (name.Equals(newInterface.Name))
                                {
                                    adapterConnection.SetValue("Name", Config.OpenVpnTapName, RegistryValueKind.String);
                                    found = true;
                                    break;
                                }
                            }
                        } 
                    }
                }

                if (!found)
                {
                    TextEvent("Installed OpenVPN interface not found.", true);
                    installResult = false;
                }
            }

            return installResult;
        }

        public static bool Uninstall(string driverDir)
        {
            uninstallResult = false;

            VerifyDriverFiles(driverDir);

            string tapInstallFilename = Path.Combine(driverDir, "tapinstall.exe");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = tapInstallFilename,
                Arguments = "remove " + Config.OpenVpnTapId,
                WorkingDirectory = driverDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process tapInstall = new Process();
            tapInstall.StartInfo = psi;
            tapInstall.ErrorDataReceived += tapInstall_ErrorDataReceived;
            tapInstall.OutputDataReceived += tapInstall_OutputDataReceived;
            tapInstall.EnableRaisingEvents = true;
            tapInstall.Start();
            tapInstall.BeginOutputReadLine();
            tapInstall.BeginErrorReadLine();

            try
            {
                while (!tapInstall.WaitForExit(500))
                {
                    if (uninstallResult)
                    {
                        tapInstall.WaitForExit(2000);
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Log.Warn(ex.Message, ex);
            }

            if (!tapInstall.HasExited)
            {
                tapInstall.Kill();
            }

            return !IsInterfaceExists();
        }

        private static void tapInstall_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string text = e.Data;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (text.Contains("Drivers installed successfully."))
            {
                installResult = true;
            }

            if (text.Contains("were removed."))
            {
                uninstallResult = true;
            }

            TextEvent(text);
        }

        private static void tapInstall_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string text = e.Data;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            installError = true;

            TextEvent(text, true);
        }

        private static void TextEvent(string text, bool isError = false)
        {
            if (OnInstallTextMessage != null)
            {
                OnInstallTextMessage(null, new TextEventArgs(text));
            }
            else
            {
                if (isError)
                {
                    Logger.Log.Error(text);
                }
                else
                {
                    Logger.Log.Info(text);
                }
            }
        }
    }
}
