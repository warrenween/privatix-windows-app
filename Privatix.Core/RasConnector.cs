using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using DotRas;


namespace Privatix.Core
{
    public class RasConnector : IConnector, IDisposable
    {
        private string host = "";
        private RasHandle rasHandle;
        private RasVpnStrategy Strategy = RasVpnStrategy.IkeV2Only;
        private InternalServerEntry entry;
        private readonly RasDialer rasDialer = new RasDialer();
        private readonly RasConnectionWatcher watcher = new RasConnectionWatcher();

        private RasHandle handle;
        public RasHandle Handle
        {
            get { return handle; }
            set
            {
                handle = value;
                watcher.Handle = handle;
            }
        }


        public RasConnector(InternalServerEntry serverEntry)
        {
            entry = serverEntry;
            host = entry.CurrentDomen;

            watcher.Disconnected += WatcherOnDisconnected;
            if (entry.ConnectStatus != InternalStatus.Disconnecting)
            {
                watcher.Connected += WatcherOnConnected;
            }
            watcher.EnableRaisingEvents = true;

            rasDialer.DialCompleted += DoDialCompleted;
            rasDialer.StateChanged += DoStateChanged;
            rasDialer.Error += OnError;
        }

        private void WatcherOnConnected(object sender, RasConnectionEventArgs args)
        {
            Logger.Log.Info("Ras-connected-watcher: connected " + args.Connection.EntryName);
            if (args.Connection.EntryName != Config.RasEntryName) 
                return;

            GlobalEvents.Connected(this, entry);

            watcher.Connected -= WatcherOnConnected;
        }

        private void WatcherOnDisconnected(object sender, RasConnectionEventArgs args)
        {
            Logger.Log.Info("Ras-connected-watcher: disconnected " + args.Connection.EntryName);
            if (args.Connection.EntryName != Config.RasEntryName) 
                return;
            GlobalEvents.Disconnected(this, entry);

            Close();
        }

        public void Close()
        {
            try
            {
                watcher.Connected -= WatcherOnConnected;
                watcher.Disconnected -= WatcherOnDisconnected;
                rasDialer.DialCompleted -= DoDialCompleted;
                rasDialer.StateChanged -= DoStateChanged;
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            Close();
        }

        public bool DoConnect()
        {
            Logger.Log.Info("Starting IKEv2 connection to " + host);
            GlobalEvents.Connecting(this, entry);

            entry.ConnectionLogClear();
            entry.LastError = "";

            try
            {

                NetworkCredential networkCredential = new NetworkCredential(entry.Login, entry.Password);

                //add route for vpn server
                IPAddress[] servers = DnsResolver.ResolveHost(host);
                if (servers == null) 
                    throw new Exception("Error connecting to DNS server");
                if (servers.Length == 0) 
                    throw new Exception("Error resolve server address");

                foreach (IPAddress ip in servers)
                {
                    Guard.Instance.ClearServerIpRoutes(ip);
                }
                
                if (Config.ConnectionGuard && Guard.Instance.IsGuardEnabled)
                {
                    IPAddress gateway = Guard.Instance.DefaultNetwork.Gateway;
                    uint ifIndex = Guard.Instance.DefaultNetwork.IfIndex;

                    foreach (IPAddress ip in servers)
                    {
                        Guard.Instance.AddRoute(ip, IPAddress.None, gateway, ifIndex, 5);
                    }

                    Guard.Instance.AddDefaultNetworkDns();
                }

                string phoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User);

                using (var phoneBook = new RasPhoneBook())
                {

                    phoneBook.Open(phoneBookPath);
                    Logger.Log.Info("Phonebook open");

                    bool doAdd = false;

                    // REQUIRED PARAMS:
                    //+ PhoneNumber 
                    //+ DeviceType 
                    //+ DeviceName 
                    //+ FramingProtocol 
                    //+ EntryType

                    RasEntry re;
                    if (phoneBook.Entries.Contains(Config.RasEntryName))
                    {
                        Logger.Log.Info("Found existing Ras-entry");
                        re = phoneBook.Entries[Config.RasEntryName];
                        var dev = RasDevice.GetDevices().FirstOrDefault(d => d.Name.Contains(Config.RasDeviceName));
                        if (dev == null)
                        {
                            Logger.Log.Info("Creating new RasDevice-entry");
                            re.Device = RasDevice.Create(Config.RasDeviceName, RasDeviceType.Vpn);
                        }
                        else
                        {
                            Logger.Log.Info("Using existing RasDevice-entry");
                            re.Device = dev;
                        }
                    }
                    else
                    {
                        Logger.Log.Info("Creating new Ras-entry");
                        re = new RasEntry(Config.RasEntryName) { Device = RasDevice.Create(Config.RasDeviceName, RasDeviceType.Vpn) };
                        doAdd = true;
                    }
                    
                    re.EntryType = RasEntryType.Vpn;
                    re.NetworkProtocols.IP = true;
                    re.VpnStrategy = Strategy;
                    re.FramingProtocol = RasFramingProtocol.Ppp;
                    re.EncryptionType = RasEncryptionType.Optional;
                    re.PhoneNumber = host;
                    re.IPv4InterfaceMetric = 1;

                    re.Options.CustomEncryption = false;
                    re.Options.CustomScript = false;
                    re.Options.DisableClassBasedStaticRoute = false;
                    re.Options.DisableIkeNameEkuCheck = false;
                    re.Options.DisableLcpExtensions = false;
                    re.Options.DisableMobility = false;
                    re.Options.DisableNbtOverIP = false;
                    re.Options.DoNotNegotiateMultilink = true;
                    re.Options.DoNotUseRasCredentials = false;
                    re.Options.Internet = true;
                    re.Options.IPHeaderCompression = false;
                    re.Options.IPv6RemoteDefaultGateway = false;
                    re.Options.ModemLights = true;
                    re.Options.NetworkLogOn = false;
                    re.Options.PreviewDomain = false;
                    re.Options.PreviewPhoneNumber = false;
                    re.Options.PreviewUserPassword = false;
                    re.Options.PromoteAlternates = false;
                    re.Options.ReconnectIfDropped = true;
                    re.Options.RegisterIPWithDns = false;
                    re.Options.RemoteDefaultGateway = true;
                    re.Options.RequireChap = false;
                    re.Options.RequireDataEncryption = false;
                    re.Options.RequireEap = true;
                    re.Options.RequireEncryptedPassword = false;
                    re.Options.RequireMachineCertificates = false;
                    re.Options.RequireMSChap = false;
                    re.Options.RequireMSChap2 = false;
                    re.Options.RequireMSEncryptedPassword = false;
                    re.Options.RequirePap = false;
                    re.Options.RequireSpap = false;
                    re.Options.RequireWin95MSChap = false;
                    re.Options.SecureClientForMSNet = false;
                    re.Options.SecureFileAndPrint = false;
                    re.Options.SecureLocalFiles = false;
                    re.Options.SecureRoutingCompartment = false;
                    re.Options.SharedPhoneNumbers = false;
                    re.Options.SharePhoneNumbers = false;
                    re.Options.ShowDialingProgress = false;
                    re.Options.SoftwareCompression = false;
                    re.Options.TerminalAfterDial = false;
                    re.Options.TerminalBeforeDial = false;
                    re.Options.UseCountryAndAreaCodes = false;
                    re.Options.UseDnsSuffixForRegistration = false;
                    re.Options.UseGlobalDeviceSettings = false;
                    re.Options.UseLogOnCredentials = false;
                    re.Options.UsePreSharedKey = false;
                    re.Options.UseTypicalSettings = false;


                    if (doAdd)
                        phoneBook.Entries.Add(re);

                    re.UpdateCredentials(networkCredential);

                    Logger.Log.Info("Updating Ras-entry...");
                    if (re.Update())
                    {
                        Logger.Log.Info("Entry updated");
                    }
                    else
                    {
                        Logger.Log.Error("Entry not updated!");
                    }
                }

                rasDialer.EntryName = Config.RasEntryName;
                rasDialer.Timeout = 20000;
                rasDialer.Credentials = networkCredential;
                //rasDialer.AutoUpdateCredentials = RasUpdateCredential.User;

                rasDialer.PhoneBookPath = phoneBookPath;

                Logger.Log.Info("Starting dial...");
                rasHandle = rasDialer.DialAsync();
                Logger.Log.Info("Dialing...");
                return true;
            }
            catch (Exception ex)
            {
                GlobalEvents.Disconnected(this, entry);
                Logger.Log.Error("DoConnect error", ex);
                
                return false;
            }
        }

        private string GetStatus(RasConnectionState state)
        {
            switch (state)
            {
                case RasConnectionState.AllDevicesConnected: return "The devices within the device chain have all connected, a physical link has been established.";
                case RasConnectionState.ApplySettings: return "The client is applying settings.";
                case RasConnectionState.AuthAck: return "The authentication request is being acknowledged.";
                case RasConnectionState.AuthCallback: return "The remote access server has requested a callback number.";
                case RasConnectionState.AuthChangePassword: return "The client has requested to change the password on the account.";
                case RasConnectionState.AuthLinkSpeed: return "The link speed calculation phase is starting.";
                case RasConnectionState.AuthNotify: return "An authentication event has occurred.";
                case RasConnectionState.AuthProject: return "The projection phase is starting.";
                case RasConnectionState.AuthRetry: return "The client has requested another authentication attempt.";
                case RasConnectionState.Authenticate: return "The authentication process is starting.";
                case RasConnectionState.Authenticated: return "The client has successfully completed authentication.";
                case RasConnectionState.CallbackComplete: return "The client has been called back and is about to resume authentication.";
                case RasConnectionState.CallbackSetByCaller: return "The client has entered the callback state.";
                case RasConnectionState.ConnectDevice: return "The device is about to be connected.";
                case RasConnectionState.Connected: return "The client has connected successfully.";
                case RasConnectionState.DeviceConnected: return "The device has connected successfully.";
                case RasConnectionState.Disconnected: return "The client has disconnected or failed a connection attempt.";
                case RasConnectionState.Interactive: return "The client has entered an interactive state.";
                case RasConnectionState.InvokeEapUI: return "The client has been paused to display a custom authentication user interface.";
                case RasConnectionState.LogOnNetwork: return "The client is logging on to the network.";
                case RasConnectionState.OpenPort: return "The communications port is about to be opened.";
                case RasConnectionState.PasswordExpired: return "The client password has expired.";
                case RasConnectionState.PortOpened: return "The communications port has been opened successfully.";
                case RasConnectionState.PostCallbackAuthentication: return "The authentication (after callback) phase is starting.";
                case RasConnectionState.PrepareForCallback: return "The line is about to disconnect in preparation for callback.";
                case RasConnectionState.Projected: return "The projection result information has been made available.";
                case RasConnectionState.RetryAuthentication: return "The client is starting to retry authentication.";
                case RasConnectionState.StartAuthentication: return "The user authentication is being started or reattempted.";
                case RasConnectionState.SubEntryConnected: return "The subentry within a multi-link connection has been connected.";
                case RasConnectionState.SubEntryDisconnected: return "The subentry within a multi-link connection has been disconnected.";
                case RasConnectionState.WaitForCallback: return "The client is waiting for an incoming call from the remote access server.";
                case RasConnectionState.WaitForModemReset: return "The client is delaying in order to give the modem time to reset itself in preparation for callback.";
            }
            return "<unknown> " + state.ToString();
        }

        private void DoStateChanged(object sender, StateChangedEventArgs args)
        {
            
            if (args.ErrorCode != 0)
            {
                string errorMessage = args.ErrorMessage;
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = new Win32Exception(args.ErrorCode).Message;
                    if (errorMessage == null)
                    {
                        errorMessage = "";
                    }
                }
                string logString = string.Format("{0} [error: 0x{1:X8}/error-ext: {2:X8}] err-msg: {3}", GetStatus(args.State), args.ErrorCode, args.ExtendedErrorCode, args.ErrorMessage);
                Logger.Log.Error(logString);
                entry.ConnectionLogAddLine(logString);
                entry.LastError = logString;
            }
            else
            {
                string logString = GetStatus(args.State);
                Logger.Log.Info(logString);
                entry.ConnectionLogAddLine(logString);
            }

            if (args.State == RasConnectionState.Disconnected)
            {
                GlobalEvents.Disconnected(this, entry);
                if (string.IsNullOrEmpty(entry.LastError))
                {
                    entry.LastError = "Disconnected";
                }
            }
        }

        private void DoDialCompleted(object sender, DialCompletedEventArgs args)
        {
            if (args.TimedOut)
            {
                Logger.Log.Error("Connection time out!");
                return;
            }

            if (args.Connected)
            {
                try
                {
                    watcher.Handle = rasHandle;

                    Logger.Log.Info("Connected");
                    GlobalEvents.Connected(this, entry);

                    if (Config.ConnectionGuard && Guard.Instance.IsGuardEnabled)
                    {
                        Guard.Instance.DeleteDefaultNetworkDns();
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                    DoDisconnect();
                }
            }
            if (args.Handle == null || args.Handle.IsClosed || args.Handle.IsInvalid)
            {
                Logger.Log.Error("Disconnected on complete");
                GlobalEvents.Disconnected(this, entry);
                args.Handle.Close();
            }
        }

        private void OnError(object sender, ErrorEventArgs args)
        {
            Exception ex = args.GetException();
            Logger.Log.Error("RasError", ex);
        }

        public void DoDisconnect()
        {
            Logger.Log.Info("Disconnecting...");
            GlobalEvents.Disconnecting(this, entry);

            try
            {
                watcher.Disconnected -= WatcherOnDisconnected;
            }
            catch (Exception) { }

            try
            {
                RasConnection conn = null;
                if (rasHandle != null)
                {
                    conn = RasConnection.GetActiveConnections().FirstOrDefault(c => c.Handle == rasHandle);
                }
                else
                {
                    conn = RasConnection.GetActiveConnections().FirstOrDefault(c => (c.EntryName == Config.RasEntryName && c.PhoneBookPath == RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User)));
                }

                if (conn == null)
                {
                    Logger.Log.Info("Connection not found.");
                    return;
                }

                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        if (conn.Handle.IsClosed)
                        {
                            Logger.Log.Info("Connection handle already closed");
                            break;
                        }

                        Logger.Log.Info("Breaking connection...");
                        conn.HangUp();
                        Logger.Log.Info("OK");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
            finally
            {
                GlobalEvents.Disconnected(this, entry);
                Close();
            }
        }

        public static bool DoAnyDisconnect()
        {
            try
            {
                RasConnection conn = RasConnection.GetActiveConnections().FirstOrDefault(c => c.EntryName == Config.RasEntryName);
                if (conn == null)
                {
                    Logger.Log.Info("Connection not found.");
                    return false;
                }

                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        if (conn.Handle.IsClosed)
                        {
                            Logger.Log.Info("Connection handle already closed");
                            return true;
                        }

                        Logger.Log.Info("Breaking connection...");
                        conn.HangUp();
                        Logger.Log.Info("OK");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }

            return false;
        }

        public static bool RemoveEntry()
        {
            try
            {
                string phoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User);

                using (var phoneBook = new RasPhoneBook())
                {
                    phoneBook.Open(phoneBookPath);
                    phoneBook.Entries.Remove(Config.RasEntryName);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
