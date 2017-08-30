using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;


namespace Privatix.Core
{
    public static class Config
    {

        #region Consts

        #region Urls

        /// <summary>
        /// Premium link url on Server List page
        /// </summary>
        public const string ServerListPremiumUrl = "https://www.privatix.com/premium/?utm_campaign=desktop_soft&utm_source=countries_screen&utm_medium=win";

        /// <summary>
        /// Premium link url on Lock screen
        /// </summary>
        public const string LockPremiumUrl = "https://www.privatix.com/premium/?utm_campaign=desktop_soft&utm_source=waiting_window&utm_medium=win";

        /// <summary>
        /// Premium link url on Diconnect Timer Banner
        /// </summary>
        public const string DisconnectBannerPremiumUrl = "https://privatix.com/premium/?tap_a=14498-aeef81&tap_s=74235-0f38be";

        /// <summary>
        /// Help link url
        /// </summary>
        public const string HelpUrl = "http://support.privatix.com";

        /// <summary>
        /// Term of use link url
        /// </summary>
        public const string TermOfUseUrl = "https://privatix.com/terms-of-service/";

        /// <summary>
        /// Privacy link url
        /// </summary>
        public const string PrivacyUrl = "https://privatix.com/privacy-policy/";

        /// <summary>
        /// Get more link url on IP Address page
        /// </summary>
        public const string IpAdressDetailsUrl = "http://ipleak.com/?utm_campaign=desktop_soft&utm_source=ipcheck_screen&utm_medium=win";

        #endregion

        /// <summary>
        /// Site API URL
        /// </summary>
#if _DEV_API_
        private const string ConstApiHost = "http://dev.privatix.net";
        private const string ApiPath = "/api/v1";
#elif _STAGE_API_
        private const string ConstApiHost = "http://stage.privatix.net";
        private const string ApiPath = "/api/v1";
#else
        private const string ConstApiHost = "https://prvtx.net";
        private const string ApiPath = "/api/v1";
#endif

        /// <summary>
        /// Reserved Site API source URL
        /// </summary>
        public const string ReservedSiteSourceUrl = "https://s3.eu-central-1.amazonaws.com/privatix-alt-name/api.txt";

        /// <summary>
        /// Reserved Site API URL encrypt key
        /// </summary>
        public const string ReservedHostEncryptIV = "9027ce06e06dbc8b8ca76d2b63eec33d";
        public const string ReservedHostEncryptKey = "5f838655d1bd6312b314d3d1c8de4fe5";

        /// <summary>
        /// Site API Version
        /// </summary>
        public const string ApiVersion = "3";

        /// <summary>
        /// Server list update period in seconds
        /// </summary>
        public const int ServerListUpdatePeriod = 15 * 60;

        /// <summary>
        /// DNS-сервер по-умолчанию
        /// </summary>
        public const string DefaultDnsServer = "8.8.8.8";

        /// <summary>
        /// Альтернативный DNS-сервер
        /// </summary>
        public const string SecondDnsServer = "8.8.4.4";

        /// <summary>
        /// Путь к каталогу с логами
        /// </summary>
        public static readonly string DefaultLogDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Privatix\\Logs\\";

        /// <summary>
        /// Имя файла с логами
        /// </summary>
        public const string LogFilename = "privatix_log.txt";

        /// <summary>
        /// Имя сетевого подключения (RasEntry)
        /// </summary>
        public const string RasEntryName = "Privatix";

        /// <summary>
        /// Имя RasDevice
        /// </summary>
        public const string RasDeviceName = "PrivatixDevice";

        /// <summary>
        /// Период проверки обновлений в секундах
        /// </summary>
        public const int UpdateDelta = 12 * 60 * 60; //раз в 12 часов

        /// <summary>
        /// URL для апдейтов (где храниться update.txt и privatix.msi)
        /// </summary>
        public const string UpdateBaseUri = "https://dxw4crzwfgmzw.cloudfront.net/win";

        /// <summary>
        /// Путь к папке куда скачивается апдейт
        /// </summary>
        public static readonly string UpdatePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Privatix\\Updates\\";

        /// <summary>
        /// Версия программы
        /// </summary>
        public static readonly string Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        /// <summary>
        /// Facebook AppId для OAuth-авторизации
        /// </summary>
        public const string FacebookAppId = "780249315436333";

        /// <summary>
        /// Facebook scope для OAuth-авторизации
        /// </summary>
        public const string FacebookScope = "email";

        /// <summary>
        /// Facebook redirect_uri для OAuth-авторизации
        /// </summary>
        public const string FacebookRedirectUri = "https://www.facebook.com/connect/login_success.html";        

        /// <summary>
        /// Google Client ID для OAuth-авторизации
        /// </summary>
        public const string GoogleClientId = "3679004947-h21dms241gfn6aep76vkjb699h0nohbi.apps.googleusercontent.com";

        /// <summary>
        /// Google Client secret для OAuth-авторизации
        /// </summary>
        public const string GoogleClientSecret = "FwefcpBY5Qu8XYhdINxHkBCw";

        /// <summary>
        /// Google scope для OAuth-авторизации
        /// </summary>
        public const string GoogleScope = "email%20profile";

        /// <summary>
        /// Google redirect_uri для OAuth-авторизации
        /// </summary>
        public const string GoogleRedirectUri = "https://privatix.com/";

        /// <summary>
        /// Google analytics tracking code
        /// </summary>
        public const string GoogleAnalyticsTrackingCode = "UA-71361845-14";

        /// <summary>
        /// Ссылка для скачивания файла из CDN для теста скорости
        /// </summary>
        public const string SpeedTestCdnUrl = "https://cdn.privatix.com/4mb.jpeg";

        /// <summary>
        /// Шаблон Ссылки для скачивания файла из ноды для теста скорости
        /// </summary>
        public const string SpeedTestNodeUrlTemplate = "http://{0}/1mb.bin?{1}";

        /// <summary>
        /// Путь в реестре относительно HKCU, где хранятся данные пользователя
        /// </summary>
        
#if _DEV_API_
        public const string RegRoot = "Software\\Privatix\\dev\\";
#elif _STAGE_API_
        public const string RegRoot = "Software\\Privatix\\stage\\";
#else
        public const string RegRoot = "Software\\Privatix\\";
#endif

        /// <summary>
        /// Путь в реестре относительно корня, где хранятся данные пользователя
        /// </summary>
        public const string RegRootFull = "HKEY_CURRENT_USER\\" + RegRoot;

        /// <summary>
        /// Путь в реестре относительно HKEY_LOCAL_MACHINE, где хранятся некоторые данные приложения
        /// </summary>
        public const string RegRootMachine = "HKEY_LOCAL_MACHINE\\" + RegRoot;

        /// <summary>
        /// Имя глобального события для активации главного окна
        /// </summary>
        public const string GlobalEventName = "__PrivatixClientGlobalEvent__";

        /// <summary>
        /// Имя глобального события для закрытия приложения
        /// </summary>
        public const string CloseEventName = "__PrivatixClientCloseEvent__";

        /// <summary>
        /// Названия программ для поиска для ивента Competitor в GA
        /// </summary>
        public static string[] Competitors = new string[] { "Tunnelbear", "Hotspot Shield", "Cyberghost", "Zenmate", "Betternet" };

        /// <summary>
        /// OpenVPN tap id
        /// </summary>
        public const string OpenVpnTapId = "tap0901";

        /// <summary>
        /// Названия интерфейса для подключения по OpenVPN
        /// </summary>
        public const string OpenVpnTapName = "Privatix TAP";

        /// <summary>
        /// Порт для подключения по OpenVPN UDP
        /// </summary>
        public const int OpenVpnUdpPort = 1194;

        /// <summary>
        /// Порт для подключения по OpenVPN TCP
        /// </summary>
        public const int OpenVpnTcpPort = 443;

        /// <summary>
        /// Порт для управления процессом OpenVPN при соединении по UDP
        /// </summary>
        public const int OpenVpnManagementUdpPort = 7505;

        /// <summary>
        /// Порт для управления процессом OpenVPN при соединении по TCP
        /// </summary>
        public const int OpenVpnManagementTcpPort = 7506;

        /// <summary>
        /// Пароль для включения режима Dev UI
        /// </summary>
        public const string DevModePassword = "privatixqweasd123";

        /// <summary>
        /// Порт для проверки установленной программы из веб-части (http)
        /// </summary>
        public const int WebCheckPort = 8099;

        /// <summary>
        /// Порт для проверки установленной программы из веб-части (https)
        /// </summary>
        public const int WebCheckPortHttps = 8098;

        #endregion


        #region Fields

        /// <summary>
        /// Start with windows
        /// </summary>
        private static bool networkAlert = true;
        private static bool dnsLeak = false;
        private static bool connectionGuard = false;
        private static bool adjustSystemTime = false;
        private static bool autoUpdate = true;
        private static bool autoRun = true;
        private static string sid = "";
        private static string subscription_id = "";
        private static string apiUrl = "";
        private static string reservedApiHost = "";
        private static string openVpnDir = "";
        private static string openVpnDriversDir = "";
        private static string logPath = "";
    
        private static Random random = new Random();

        #endregion


        #region Properties

        public static string ApiUrl
        {
            get
            {
                if (string.IsNullOrEmpty(apiUrl))
                {
                    CreateApiUrl();
                }

                return apiUrl;
            }
        }

        public static string ReservedApiHost
        {
            get
            {
                return reservedApiHost;
            }
        }

        public static bool AutoRun
        {
            get
            {
                return autoRun;
            }

            set
            {
                autoRun = value;
                Registry.SetValue(RegRootFull, "Autorun", autoRun.ToString(CultureInfo.InvariantCulture));
                Logger.Log.Info(autoRun.ToString());

                try
                {
                    if (autoRun)
                    {
                        Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", "Privatix", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
                    }
                    else
                    {
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                        {
                            if (key != null)
                            {
                                key.DeleteValue("Privatix", false);
                            }
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            }
        }

        public static bool DnsLeak
        {
            get
            {
                return dnsLeak;
            }

            set
            {
                dnsLeak = value;
                Registry.SetValue(RegRootFull, "DnsLeak", dnsLeak.ToString(CultureInfo.InvariantCulture));
                Logger.Log.Info(dnsLeak.ToString());
            }
        }

        public static bool ConnectionGuard
        {
            get
            {
                return connectionGuard;
            }

            set
            {
                connectionGuard = value;
                Registry.SetValue(RegRootFull, "Guard", connectionGuard.ToString(CultureInfo.InvariantCulture));
                Logger.Log.Info(connectionGuard.ToString());
            }
        }

        public static bool NetworkAlert
        {
            get
            {
                return networkAlert;
            }

            set
            {
                networkAlert = value;
                Registry.SetValue(RegRootFull, "NetworkAlert", networkAlert.ToString(CultureInfo.InvariantCulture));
                Logger.Log.Info(networkAlert.ToString());
            }
        }

        public static bool AdjustSystemTime
        {
            get
            {
                return adjustSystemTime;
            }

            set
            {
                adjustSystemTime = value;
                Registry.SetValue(RegRootFull, "AdjustSystemTime", adjustSystemTime.ToString(CultureInfo.InvariantCulture));
                Logger.Log.Info(adjustSystemTime.ToString());
            }
        }

        public static bool AutoUpdate
        {
            get
            {
                return autoUpdate;
            }

            set
            {
                autoUpdate = value;
                Registry.SetValue(RegRootFull, "AutoUpdate", autoUpdate.ToString(CultureInfo.InvariantCulture));
                Logger.Log.Info(autoUpdate.ToString());
            }
        }

        public static string Sid
        {
            get
            {
                return sid;
            }

            set
            {
                sid = value;
                Registry.SetValue(RegRootFull, "SID", sid);
            }
        }

        public static string Subscription_Id
        {
            get
            {
                return subscription_id;
            }

            set
            {
                subscription_id = value;
                Registry.SetValue(RegRootFull, "SubscriptionId", subscription_id);
            }
        }

        public static Random Random
        {
            get
            {
                return random;
            }
        }

        public static string OpenVpnDir
        {
            get
            {
                if (string.IsNullOrEmpty(openVpnDir))
                {
                    try
                    {
                        openVpnDir = SafeStr(Registry.GetValue(RegRootMachine, "OpenVpnDir", ""), "");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Warn(ex.Message, ex);
                    }

                    if (string.IsNullOrEmpty(openVpnDir))
                    {
                        openVpnDir = Path.Combine(typeof(Config).Assembly.Location, "openvpn\\");
                    }
                }                

                return openVpnDir;
            }
        }

        public static string OpenVpnDriversDir
        {
            get
            {
                if (string.IsNullOrEmpty(openVpnDriversDir))
                {
                    try
                    {
                        openVpnDriversDir = SafeStr(Registry.GetValue(RegRootMachine, "OpenVpnDriversDir", ""), "");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Warn(ex.Message, ex);
                    }

                    if (string.IsNullOrEmpty(openVpnDriversDir))
                    {
                        string assemblyLocation = Path.GetDirectoryName(typeof(Config).Assembly.Location);
                        openVpnDriversDir = Path.Combine(assemblyLocation, "openvpn\\drivers\\");
                    }
                }                

                return openVpnDriversDir;
            }
        }

        public static string LogPath
        {
            get
            {
                if (string.IsNullOrEmpty(logPath))
                {
                    try
                    {
                        logPath = SafeStr(Registry.GetValue(RegRootMachine, "LogsDir", ""), "");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Warn(ex.Message, ex);
                    }

                    if (string.IsNullOrEmpty(logPath))
                    {
                        try
                        {
                            logPath = DefaultLogDir;
                            if (Process.GetCurrentProcess().ProcessName.Equals("Privatix", StringComparison.CurrentCultureIgnoreCase))
                            {
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.Warn("Could not save reg value", ex);
                        }
                    }
                }

                return logPath;
            }
        }

        #endregion


        #region Methods

        public static void Load()
        {
            AutoRun = Boolean.Parse(SafeStr(Registry.GetValue(RegRootFull, "Autorun", "True"), "True"));
            DnsLeak = Boolean.Parse(SafeStr(Registry.GetValue(RegRootFull, "DnsLeak", "True"), "True"));
            ConnectionGuard = Boolean.Parse(SafeStr(Registry.GetValue(RegRootFull, "Guard", "False"), "False"));
            NetworkAlert = Boolean.Parse(SafeStr(Registry.GetValue(RegRootFull, "NetworkAlert", "True"), "True"));
            AdjustSystemTime = Boolean.Parse(SafeStr(Registry.GetValue(RegRootFull, "AdjustSystemTime", "False"), "False"));
            AutoUpdate = Boolean.Parse(SafeStr(Registry.GetValue(RegRootFull, "AutoUpdate", "True"), "True"));

            sid = SafeStr(Registry.GetValue(RegRootFull, "SID", ""), "");
            subscription_id = SafeStr(Registry.GetValue(RegRootFull, "SubscriptionId", ""), "");

            reservedApiHost = SafeStr(Registry.GetValue(RegRootFull, "ReservedApiHost", ""), "");
        }

        public static void Save()
        {
            Registry.SetValue(RegRootFull, "Autorun", autoRun.ToString(CultureInfo.InvariantCulture));
            Registry.SetValue(RegRootFull, "DnsLeak", dnsLeak.ToString(CultureInfo.InvariantCulture));
            Registry.SetValue(RegRootFull, "Guard", connectionGuard.ToString(CultureInfo.InvariantCulture));
            Registry.SetValue(RegRootFull, "NetworkAlert", networkAlert.ToString(CultureInfo.InvariantCulture));
            Registry.SetValue(RegRootFull, "AdjustSystemTime", adjustSystemTime.ToString(CultureInfo.InvariantCulture));
            Registry.SetValue(RegRootFull, "AutoUpdate", autoUpdate.ToString(CultureInfo.InvariantCulture));
            Registry.SetValue(RegRootFull, "SID", sid);
            Registry.SetValue(RegRootFull, "SubscriptionId", subscription_id);
        }

        private static string SafeStr(object obj, string def)
        {
            if (obj == null)
                return def;
            return obj.ToString();
        }

        public static string GetWindowsVersion()
        {
            string version = "";

            try
            {
                
                ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * From Win32_OperatingSystem");
                foreach (ManagementObject getserial in MOS.Get())
                {
                    var meth = getserial.GetPropertyValue("Caption");
                    if (meth != null)
                        version = meth.ToString();
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                version = "";
            }
            finally
            {
                if (string.IsNullOrEmpty(version))
                {
                    version = Environment.OSVersion.VersionString;
                }
            }            

            return version;
        }

        public static string GetFullWindowsVersion()
        {
            string caption = GetWindowsVersion();
            string result = ""; ;
            string servicePack = "";
            string build = "";
            string bits = "";
            try
            {
                ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * From Win32_OperatingSystem");
                foreach (ManagementObject mac in MOS.Get())
                {
                    try
                    {
                        var sp = mac.GetPropertyValue("ServicePackMajorVersion");
                        if (sp != null && sp is ushort)
                        {
                            ushort spack = (ushort)sp;
                            if (spack != 0)
                                servicePack = " SP" + spack.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                        servicePack = "";
                    }

                    try
                    {
                        var b = mac.GetPropertyValue("BuildNumber");
                        if (b != null)
                            build = " build " + b.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                        build = "";
                    }

                    try
                    {
                        var arch = mac.GetPropertyValue("OSArchitecture");
                        if (arch != null)
                            bits = " " + arch;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                        bits = "";
                    }
                }

                result = caption + build + servicePack + bits;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                result = "";
            }
            finally
            {
                if (string.IsNullOrEmpty(result))
                {
                    result = GetWindowsVersion();
                }
            }            

            return result;
        }

        public static string GetDeviceName()
        {
            string deviceName = "";

            try
            {
                ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * From Win32_ComputerSystem");
                foreach (ManagementObject mac in MOS.Get())
                {
                    var meth = mac.GetPropertyValue("Manufacturer");
                    if (meth != null)
                        deviceName = meth.ToString();

                    try
                    {
                        meth = mac.GetPropertyValue("Model");
                        if (meth != null)
                            deviceName += " " + meth.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex.Message, ex);
                    }

                    if (!string.IsNullOrEmpty(deviceName))
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);                
            }
            finally
            {
                if (string.IsNullOrEmpty(deviceName))
                {
                    deviceName = Environment.MachineName;
                }
            }

            return deviceName;
        }

        public static string GetDeviceId()
        {
            string mbSerial = Config.GetMotherBoardSerial();
            string procId = Config.GetProcessorID();
            string deviceId = mbSerial + "*" + procId;
            if (string.IsNullOrEmpty(mbSerial) && string.IsNullOrEmpty(procId))
            {
                deviceId = GetDeviceName();
            }
            var uuid = Config.GetMD5Hash(deviceId);

            return uuid;

        }

        public static string GetMotherBoardSerial()
        {
            string mbCode = "";
            try
            {
                ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * From Win32_BaseBoard");
                foreach (ManagementObject getserial in MOS.Get())
                {
                    var meth = getserial.GetPropertyValue("SerialNumber");
                    if (meth != null)
                        mbCode = meth.ToString();
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            return mbCode;
        }

        public static string GetProcessorID()
        {
            string cpuCode = "";
            try
            {
                ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * From Win32_processor");
                foreach (ManagementObject getPID in MOS.Get())
                {
                    var meth = getPID.GetPropertyValue("ProcessorID");
                    if (meth != null)
                        cpuCode += meth.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            return cpuCode;
        }

        public static string GetMD5Hash(string input)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            return hash.Aggregate("", (c, n) => c + n.ToString("X2"));
        }

        private static void CreateApiUrl()
        {
            if (!string.IsNullOrEmpty(reservedApiHost))
            {
                reservedApiHost = reservedApiHost.ToLower();
                if (reservedApiHost.IndexOf("http://") == 0
                    || reservedApiHost.IndexOf("https://") == 0)
                {
                    apiUrl = reservedApiHost + ApiPath;
                }
                else
                {
                    apiUrl = "https://" + reservedApiHost + ApiPath;
                }
            }
            else
            {
                apiUrl = ConstApiHost + ApiPath;
            }
        }

        public static void UpdateReservedApiHost(string host)
        {
            reservedApiHost = host;
            Registry.SetValue(RegRootFull, "ReservedApiHost", reservedApiHost);
            CreateApiUrl();
        }

        #endregion
    }
}
