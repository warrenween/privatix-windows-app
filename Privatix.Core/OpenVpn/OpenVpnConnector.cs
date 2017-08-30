using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Privatix.Core;


namespace Privatix.Core.OpenVpn
{
    public class OpenVpnConnector : IConnector, IDisposable
    {
        private InternalServerEntry entry;
        private OpenVpnHelper helper;
        private OpenVpnReceiver receiver;
        private string host;
        private Timer waitAnswerTimer;
        private string configFilename;
        private string authFilename;
        private OpenVpnProto openVpnProto;

        public OpenVpnConnector(InternalServerEntry serverEntry, OpenVpnProto proto)
        {
            entry = serverEntry;
            host = entry.CurrentDomen;
            openVpnProto = proto;

            helper = OpenVpnHelper.Instance;
            receiver = new OpenVpnReceiver();
            receiver.OutputDataReceived += OnOutputDataReceived;
            receiver.ErrorDataReceived += OnErrorDataReceived;
            receiver.ProcessExited += OnProcessExited;
            helper.OnOutputDataReceived += receiver.OnOutputDataReceived;
            helper.OnErrorDataReceived += receiver.OnErrorDataReceived;
            helper.OnProcessExited += receiver.OnProcessExited;            
        }

        void OnProcessExited(object sender, EventArgs e)
        {
            try
            {
                waitAnswerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception) { }

            CloseConnection();
        }

        void OnErrorDataReceived(object sender, Delegates.TextEventArgs args)
        {
            try
            {
                waitAnswerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception) { }

            if (string.IsNullOrEmpty(args.Text))
            {
                return;
            }

            Logger.Log.Error(args.Text);
            entry.ConnectionLogAddLine(args.Text);
            entry.LastError = args.Text;
        }

        void OnOutputDataReceived(object sender, Delegates.TextEventArgs args)
        {
            try
            {
                waitAnswerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception) { }
            
            if (string.IsNullOrEmpty(args.Text))
            {
                return;
            }

            Logger.Log.Info(args.Text);
            entry.ConnectionLogAddLine(args.Text);

            if (args.Text.Contains("route addition failed"))
            {
                CloseConnection();
                return;
            }
            else if (args.Text.Contains("Initialization Sequence Completed With Errors"))
            {
                CloseConnection();
                return;
            }
            else if (args.Text.Contains("Initialization Sequence Completed"))
            {
                GlobalEvents.Connected(this, entry);
                DeleteAuthFile();
            }

            if (args.Text.Contains("CreateFile failed on TAP device") || (args.Text.ToLower().Contains("exiting")))
            {
                CloseConnection();
                return;
            }

            if (args.Text.Contains("process restarting"))
            {
                CloseConnection();
                Thread.Sleep(1000);
                helper.CloseAllProcesses();
                return;
            }
        }

        private void CloseConnection()
        {
            Close();
            GlobalEvents.Disconnected(this, entry);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            try
            {
                receiver.OutputDataReceived -= OnOutputDataReceived;
                receiver.ErrorDataReceived -= OnErrorDataReceived;
                receiver.ProcessExited -= OnProcessExited;
                helper.OnOutputDataReceived -= receiver.OnOutputDataReceived;
                helper.OnErrorDataReceived -= receiver.OnErrorDataReceived;
                helper.OnProcessExited -= receiver.OnProcessExited;
                helper.Disconnect(openVpnProto == OpenVpnProto.TCP ? Config.OpenVpnManagementTcpPort : Config.OpenVpnManagementUdpPort);

                waitAnswerTimer.Dispose();

                DeleteConfigFile();
                DeleteAuthFile();
            }
            catch (Exception ) {}
        }

        public bool DoConnect()
        {
            Logger.Log.InfoFormat("Starting OpenVPN {0} connection to {1}", openVpnProto.ToString(), host );
            GlobalEvents.Connecting(this, entry);

            entry.ConnectionLogClear();
            entry.LastError = "";

            if (!DriverInstaller.IsInterfaceExists())
            {
                Logger.Log.Warn("OpenVPN interface not found. Start drivers reinstall.");
                if (!helper.Install())
                {
                    GlobalEvents.Disconnected(this, entry);
                    Logger.Log.Error("DoConnect error");

                    return false;
                }
            }

            //add route for vpn server
            IPAddress[] servers = DnsResolver.ResolveHost(host);
            if (servers == null)
            {
                Logger.Log.Error("Error connecting to DNS server");
                return false;
            }
            if (servers.Length == 0)
            {
                Logger.Log.Error("Error resolve server address");
                return false;
            }

            foreach (IPAddress ip in servers)
            {
                Guard.Instance.ClearServerIpRoutes(ip);
            }

            IPAddress server = servers[0];

            if (Config.ConnectionGuard && Guard.Instance.IsGuardEnabled)
            {
                IPAddress gateway = Guard.Instance.DefaultNetwork.Gateway;
                uint ifIndex = Guard.Instance.DefaultNetwork.IfIndex;
                Guard.Instance.AddRoute(server, IPAddress.None, gateway, ifIndex, 5);
            }

            //prepair config
            CreateConfigFile(server.ToString(), openVpnProto);
            CreateAuthFile(entry.Login, entry.Password);
            if (string.IsNullOrEmpty(configFilename) || string.IsNullOrEmpty(authFilename))
            {
                Logger.Log.Error("OpenVPN connect failed");
                return false;
            }


            waitAnswerTimer = new Timer(TimerCallback);
            waitAnswerTimer.Change(10000, Timeout.Infinite);
            bool result = helper.Connect(configFilename, authFilename);
            return result;
        }

        private void CreateConfigFile(string host, OpenVpnProto protocol)
        {
            string proto;
            string port;
            string managmentPort;
            if (protocol == OpenVpnProto.UDP)
            {
                proto = "udp";
                port = Config.OpenVpnUdpPort.ToString();
                managmentPort = Config.OpenVpnManagementUdpPort.ToString();
            }
            else if (protocol == OpenVpnProto.TCP)
            {
                proto = "tcp-client";
                port = Config.OpenVpnTcpPort.ToString();
                managmentPort = Config.OpenVpnManagementTcpPort.ToString();
            }
            else
            {
                Logger.Log.Error("Unknown OpenVpn proto: " + protocol.ToString());
                configFilename = null;
                return;
            }

            string config = OpenVpnConfig.Text.Replace("{host}", host).Replace("{proto}", proto).Replace("{port}", port).Replace("{managment-port}", managmentPort);

            string configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Privatix\\Config\\";
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            configFilename = Path.Combine(configDir, "client.ovpn");
            int i = 0;
            bool saved = false;
            do
            {
                try
                {
                    File.WriteAllText(configFilename, config);
                    saved = true;
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log.Warn(ex.Message, ex);
                }

                if (!saved)
                {
                    i++;
                    configFilename = Path.Combine(configDir, "client(" + i.ToString() + ").ovpn");
                } 
            } while (i <= 10 && !saved);

            if (!saved)
            {
                Logger.Log.Error("Could not save OpenVPN config file");
                configFilename = null;
            }
        }

        private void CreateAuthFile(string login, string pass)
        {
            string authFile = string.Format("{0}\n{1}\n", login, pass);

            string configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Privatix\\Config\\";
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            authFilename = Path.Combine(configDir, "auth.txt");
            int i = 0;
            bool saved = false;
            do
            {
                try
                {
                    File.WriteAllText(authFilename, authFile);
                    saved = true;
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log.Warn(ex.Message, ex);
                }

                if (!saved)
                {
                    i++;
                    authFilename = Path.Combine(configDir, "auth(" + i.ToString() + ").txt");
                }
            } while (i <= 10 && !saved);

            if (!saved)
            {
                Logger.Log.Error("Could not save OpenVPN auth file");
                authFilename = null;
            }
        }

        private void DeleteConfigFile()
        {
            if (!string.IsNullOrEmpty(configFilename) && File.Exists(configFilename))
            {
                try
                {
                    File.Delete(configFilename);
                    configFilename = null;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }               
            }
        }

        private void DeleteAuthFile()
        {
            if (!string.IsNullOrEmpty(authFilename) && File.Exists(authFilename))
            {
                try
                {
                    File.Delete(authFilename);
                    authFilename = null;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            }
        }

        public void DoDisconnect()
        {
            Logger.Log.Info("Disconnecting...");
            GlobalEvents.Disconnecting(this, entry);

            helper.Disconnect(openVpnProto == OpenVpnProto.TCP ? Config.OpenVpnManagementTcpPort : Config.OpenVpnManagementUdpPort);
        }

        private void TimerCallback(object state)
        {
            Logger.Log.Error("OpenVPN process not answer. Do disconnect.");
            CloseConnection();
        }

        public static bool DoAnyDisconnect()
        {
            return OpenVpnHelper.Instance.CloseAllProcesses();
        }
    }
}
