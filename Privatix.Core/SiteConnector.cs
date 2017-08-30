using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Bugsnag;
using Bugsnag.Clients;
using Newtonsoft.Json;
using Privatix.Core.Delegates;
using Privatix.Core.Notification;
using Privatix.Core.Site;


namespace Privatix.Core
{
    public class SiteConnector
    {
        #region Fields

        private static SiteConnector instance = null;
        private string currentIp = "Unknown";
        private string currentCountry = "Unknown";
        private string originalcountry = "";
        private bool isAuthorized = false;
        private string plan = "free";
        private bool isFree = true;
        private List<InternalServerEntry> servers = new List<InternalServerEntry>();
        private string login = "";
        private string password = "";
        private string subscription_id = "";
        private string email = "";
        private DateTime premiumEnds = DateTime.MinValue;
        private List<INotification> notificationsList = new List<INotification>();

        private int getSessionAttemps = 0;
        private bool sessionUpdating = false;
        private object lockFillSubscription = new object();
        private object lockNotifications = new object();
        private Timer updateSessionTimer = null;
        private DateTime lastErrorPost = DateTime.MinValue;
        private SyncConnector syncConnector = SyncConnector.Instance;

        #endregion


        #region Properties

        public static SiteConnector Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SiteConnector();
                }

                return instance;
            }
        }

        public string CurrentIp
        {
            get
            {
                return currentIp;
            }
        }

        public string CurrentCountry
        {
            get
            {
                return currentCountry;
            }
        }

        public string OriginalCountry
        {
            get
            {
                return originalcountry;
            }
        }

        public bool IsAuthorized
        {
            get
            {
                return isAuthorized;
            }
        }

        public string Plan
        {
            get
            {
                return plan;
            }
        }

        public bool IsFree
        {
            get
            {
                return isFree;
            }
        }

        public IList<InternalServerEntry> Servers
        {
            get
            {
                return servers.AsReadOnly();
            }
        }

        public string Login
        {
            get
            {
                return login;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }
        }

        public string Subscription_id
        {
            get
            {
                return subscription_id;
            }
        }

        public string Email
        {
            get
            {
                return email;
            }
        }

        public DateTime PremiumEnds
        {
            get
            {
                return premiumEnds;
            }
        }

        #endregion


        #region Ctors

        public SiteConnector()
        {
            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnEnded += GlobalEvents_OnEnded;
        }

        #endregion


        #region Methods

        public bool GetSession()
        {
            SessionResponse response = null;
            ErrorResponse errorResponse = null;
            bool result = false;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/user/session");
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                webRequest.Timeout = 20 * 1000;

                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    Logger.Log.Error(wex.Message, wex);
                    webResponse = (HttpWebResponse)wex.Response;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                    throw ex;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("session");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    //string answ = sr.ReadLine();
                    //Logger.Log.InfoFormat("Get Session Response: {0}", answ);
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK) //200
                    {
                        response = (SessionResponse)serializer.Deserialize(sr, typeof(SessionResponse));
                        //response = JsonConvert.DeserializeObject<SessionResponse>(answ);
                        result = true;
                    }
                    else if (webResponse.StatusCode == HttpStatusCode.Forbidden) //403
                    {
                        Config.Sid = "";
                        return PostActivation();
                    }
                    else
                    {
                        errorResponse = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));

                        LogError(errorResponse, webResponse.StatusCode);           
                    }
                }
                getSessionAttemps = 0;
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);

                if (GetReservedApiHost())
                {
                    getSessionAttemps = 0;
                    return GetSession();
                }

                return false;
            }

            if (result && response != null)
            {
                if (!string.IsNullOrEmpty(response.current_ip)
                    && !string.IsNullOrEmpty(response.country))
                {
                    if (currentIp != response.current_ip
                        || currentCountry != response.country)
                    {
                        currentIp = response.current_ip;
                        currentCountry = response.country;
                        GlobalEvents.IpInfoUpdated(this);
                    }
                }

                if (!string.IsNullOrEmpty(response.original_country)
                    && string.IsNullOrEmpty(originalcountry))
                {
                    originalcountry = response.original_country;
                }

                try
                {
                    long time_remain = response.subscription.quotes.expires_at - response.server_time;
                    long endTime = (long)((DateTime.Now - new DateTime (1970, 1, 1)).TotalSeconds + time_remain);
                    premiumEnds = new System.DateTime(1970, 1, 1).AddSeconds(endTime);
                }
                catch
                {

                }

                isAuthorized = response.is_authorized;
                email = response.email;
                
                if (response.notifications != null)
                {
                    FillNotifications(response.notifications);
                }
                else
                {
                    GetNotifications();
                }

                FillSubscription(response.subscription);
            }
            else
            {
                return PostActivation();
            }

            return result;
        }

        public bool PostActivation()
        {
            ActivationRequest request = new ActivationRequest();
            ActivationResponse response = null;
            ErrorResponse errorResponse = null;
            bool result = false;

            try
            {
                request.device = new Device();
                request.device.name = Config.GetFullWindowsVersion();
                request.device.type = "Desktop";
                request.device.model = Config.GetDeviceName();
                request.device.device_id = Config.GetDeviceId();
                request.device.os = new OperationSystem();

                request.device.os.name = Config.GetWindowsVersion();
                request.device.os.version = Environment.OSVersion.Version.ToString(2);
                request.device.os.family = "Windows";
                request.software = new Software();
                request.software.type = "msi";
                request.software.source = "site";
                request.software.version = Config.Version;

                //string json = JsonConvert.SerializeObject(request, Formatting.Indented);


                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/user/activation");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("activation");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    //string answ = sr.ReadLine();
                    //Logger.Log.InfoFormat("Activation response: {0}", answ);
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (ActivationResponse)serializer.Deserialize(sr, typeof(ActivationResponse));
                        //response = JsonConvert.DeserializeObject<ActivationResponse>(answ);
                        result = true;
                    }
                    else
                    {
                        errorResponse = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));
                        if (webResponse.StatusCode == HttpStatusCode.Forbidden 
                            && errorResponse.error_code == 4033 
                            && errorResponse.message != null
                            && string.Compare(errorResponse.message, "This software blocked", true) == 0)
                        {
                            GlobalEvents.ForceUpdate(this);
                        }
                        else
                        {
                            LogError(errorResponse, webResponse.StatusCode);
                        }
                    }
                }
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                if (GetReservedApiHost())
                {
                    return PostActivation();
                }
            }

            if (result && response != null)
            {
                if (!string.IsNullOrEmpty(response.sid))
                {
                    Config.Sid = response.sid;
                }

                if (!string.IsNullOrEmpty(response.current_ip)
                    && !string.IsNullOrEmpty(response.country))
                {
                    if (currentIp != response.current_ip
                        || currentCountry != response.country)
                    {
                        currentIp = response.current_ip;
                        currentCountry = response.country;
                        GlobalEvents.IpInfoUpdated(this);
                    }
                }

                if (!string.IsNullOrEmpty(response.country)
                    && string.IsNullOrEmpty(originalcountry))
                {
                    originalcountry = response.country;
                }

                isAuthorized = false;

                if (response.notifications != null)
                {
                    FillNotifications(response.notifications);
                }
                else
                {
                    GetNotifications();
                }

                FillSubscription(response.subscription);
            }            

            return result;
        }

        public bool PostRecover(string email, out bool recovered)
        {
            EmailRequest request = new EmailRequest();
            ErrorResponse response = null;
            bool result = false;

            request.login = email;

            recovered = false;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/user/recover");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("recover");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    //string answ = sr.ReadLine();
                    JsonSerializer serializer = new JsonSerializer();
                    response = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));
                    if (webResponse.StatusCode == HttpStatusCode.OK //200
                        || webResponse.StatusCode == HttpStatusCode.NotFound) //404
                    {
                        result = true;
                    }
                    else if (webResponse.StatusCode == HttpStatusCode.Forbidden) //403
                    {
                        Config.Sid = "";
                        PostActivation();
                    }
                }
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }

            if (response != null && !string.IsNullOrEmpty(response.status))
            {
                if (response.status == "ok")
                {
                    recovered = true;
                }
            }

            return result;
        }

        public bool PostCheckMail(string email, out bool emailFound)
        {
            EmailRequest request = new EmailRequest();
            ErrorResponse response = null;
            bool result = false;

            request.login = email;

            emailFound = false;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/user/check_mail");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("check_mail");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    // string answ = sr.ReadLine();
                    JsonSerializer serializer = new JsonSerializer();
                    response = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));
                    result = true;
                }
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }

            if (response != null && !string.IsNullOrEmpty(response.status))
            {
                if (response.status == "ok")
                {
                    emailFound = true;
                }
                else if (response.error_code == 4041)
                {
                    emailFound = false;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        public bool PostAuthorization(string login, string password, out bool authorized, out string message)
        {
            AuthorizationRequest request = new AuthorizationRequest();
            AuthorizationResponse response = null;
            ErrorResponse errorResponse = null;
            bool result = false;
            authorized = false;
            message = "";

            request.login = login;
            request.password = password;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/user/authorization");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("authorization");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    //string answ = sr.ReadLine();
                    //Logger.Log.InfoFormat("POST Auth Response: {0}", answ);
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (AuthorizationResponse)serializer.Deserialize(sr, typeof(AuthorizationResponse));
                        //response = JsonConvert.DeserializeObject<AuthorizationResponse>(answ);
                        result = true;
                    }
                    else
                    {
                        errorResponse = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));

                        if (webResponse.StatusCode == HttpStatusCode.Forbidden) //403
                        {
                            if (errorResponse.error_code == 4031)
                            {
                                message = "Device limit.";
                                GlobalEvents.ShowMessage(this, "Please visit Privatix.com website \nto deactivate one of your devices \nbefore adding another", "Device limit reached");
                                return true;
                            }
                            else
                            {
                                message = "Error. Please try again.";
                                Config.Sid = "";
                                return PostActivation();
                            }
                        }
                        else if ((int)webResponse.StatusCode == 422 && !string.IsNullOrEmpty(errorResponse.message))
                        {
                            message = errorResponse.message;
                        }
                        else
                        {
                            message = "Server error";
                            LogError(errorResponse, webResponse.StatusCode);  
                        }
                    }
                }
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                message = "Connection error";
                Logger.Log.Error(ex.Message, ex);
            }

            if (response != null)
            {
                if (!string.IsNullOrEmpty(response.sid))
                {
                    Config.Sid = response.sid;
                }
                if (response.status == "ok")
                {
                    authorized = true;
                    isAuthorized = true;
                    email = response.email;
                }

                GetNotifications();

                if (response.subscription == null)
                {
                    return GetSubscription();
                }
                else
                {
                    FillSubscription(response.subscription);
                }
            }

            return result;

        }

        public bool PostOAuth(string email, string token, OAuthProvider provider, out bool authorized, out string message)
        {
            OAuthRequest request = new OAuthRequest();
            AuthorizationResponse response = null;
            ErrorResponse errorResponse = null;
            bool result = false;
            authorized = false;
            message = "";

            request.email = email;
            request.token = token;
            request.provider = (provider == OAuthProvider.Facebook ? "fb" : "gp");

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/user/oauth");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("oauth");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    //string answ = sr.ReadLine();
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (AuthorizationResponse)serializer.Deserialize(sr, typeof(AuthorizationResponse));
                        result = true;
                    }
                    else
                    {
                        errorResponse = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));

                        if (webResponse.StatusCode == HttpStatusCode.Forbidden) //403
                        {
                            if (errorResponse.error_code == 4031)
                            {
                                message = "Device limit.";
                                GlobalEvents.ShowMessage(this, "Please visit Privatix.com website \nto deactivate one of your devices \nbefore adding another", "Device limit reached");
                                return true;
                            }
                            else
                            {
                                message = "Error. Please try again.";
                                Config.Sid = "";
                                return PostActivation();
                            }
                        }
                        else if ((int)webResponse.StatusCode == 422 && !string.IsNullOrEmpty(errorResponse.message))
                        {
                            message = errorResponse.message;
                        }
                        else
                        {
                            message = "Server error";
                            LogError(errorResponse, webResponse.StatusCode);
                        }
                    }
                }
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            if (response != null)
            {
                if (!string.IsNullOrEmpty(response.sid))
                {
                    Config.Sid = response.sid;
                }
                if (response.status == "ok")
                {
                    authorized = true;
                    isAuthorized = true;
                }

                GetNotifications();

                if (response.subscription == null)
                {
                    return GetSubscription();
                }
                else
                {
                    FillSubscription(response.subscription);
                }                
            }

            return result;

        }

        public bool PostRegistration(string email, string password, out bool registered)
        {
            AuthorizationRequest request = new AuthorizationRequest();
            AuthorizationResponse response = null;
            ErrorResponse errorResponse = null;
            bool result = false;
            registered = false;

            request.login = email;
            request.password = password;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/user/registration");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("registration");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    // string answ = sr.ReadLine();
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (AuthorizationResponse)serializer.Deserialize(sr, typeof(AuthorizationResponse));
                        result = true;
                    }
                    else
                    {
                        errorResponse = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));
                        if (webResponse.StatusCode == HttpStatusCode.BadRequest)
                        {
                            result = true;
                        }
                    }
                }
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            if (response != null)
            {
                if (!string.IsNullOrEmpty(response.sid))
                {
                    Config.Sid = response.sid;
                }

                if (response.status == "ok")
                {
                    registered = true;
                    isAuthorized = true;
                }

                GetNotifications();

                if (response.subscription == null)
                {
                    result = GetSession();
                }
                else
                {
                    FillSubscription(response.subscription);
                }                
            }

            return result;
        }

        public bool PostError(string type, int datetime, string error, string errorTrace, string connectionNode)
        {
            ErrorRequest request = new ErrorRequest();
            ErrorResponse response = null;
            bool result = false;

            request.type = type;
            request.datetime = datetime;
            request.subscription_uuid = string.IsNullOrEmpty(subscription_id) ? Config.Subscription_Id : subscription_id;
            request.error = error;
            request.error_trace = errorTrace;
            request.source_country = OriginalCountry;
            request.connection_node = connectionNode;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/error");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    // string answ = sr.ReadLine();
                    JsonSerializer serializer = new JsonSerializer();
                    response = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));
                    if (webResponse.StatusCode == HttpStatusCode.OK 
                        && response != null
                        && response.status != null
                        && string.Equals(response.status, "ok", StringComparison.InvariantCultureIgnoreCase))
                    {
                        result = true;
                    }
                }
                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            return result;
        }

        //public bool PostMetrics(MetricsType type)
        //{
        //    MetricsRequest request = new MetricsRequest();
        //    ErrorResponse response = null;

        //    request.at = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
        //    switch (type)
        //    {
        //        case MetricsType.New_install:
        //            request.type = "installation";
        //            break;
        //        case MetricsType.Enable_proxy:
        //            request.type = "select_country";
        //            break;
        //        case MetricsType.Disable_proxy:
        //            request.type = "disable";
        //            break;
        //        case MetricsType.Uninstall:
        //            request.type = "uninstall";
        //            break;
        //        case MetricsType.Open_app:
        //            request.type = "open_browser";
        //            break;
        //        case MetricsType.Registration:
        //            request.type = "registration";
        //            break;
        //        case MetricsType.Authorization:
        //            request.type = "authorization";
        //            break;
        //        default:
        //            request.type = "unknown";
        //            break;
        //    }

        //    try
        //    {
        //        HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/metrics");
        //        webRequest.Method = "POST";
        //        webRequest.ContentType = "application/json";
        //        webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
        //        webRequest.Headers.Add("X-Session-ID", Config.Sid);
        //        webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
        //        webRequest.KeepAlive = false;
        //        Stream sendStream = webRequest.GetRequestStream();
        //        using (StreamWriter sw = new StreamWriter(sendStream))
        //        {
        //            JsonSerializer serializer = new JsonSerializer();
        //            serializer.Serialize(sw, request);
        //        }
        //        sendStream.Close();
        //        HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
        //        Stream receiveStream = webResponse.GetResponseStream();
        //        using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
        //        {
        //            // string answ = sr.ReadLine();
        //            JsonSerializer serializer = new JsonSerializer();
        //            response = (ErrorResponse)serializer.Deserialize(sr, typeof(ActivationResponse));
        //        }
        //        receiveStream.Close();
        //        webResponse.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log.Error(ex.Message, ex);
        //    }

        //    return response != null && response.status.ToLower() == "ok";
        //}

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void FillSubscription(Subscription subscription)
        {
            try
            {
                bool userChanged = false;

                lock (lockFillSubscription)
                {
                    plan = subscription.plan;
                    isFree = plan.ToLower() == "free";

                    if (login != subscription.login || password != subscription.password || subscription_id != subscription.subscription_id)
                    {
                        userChanged = true;
                    }

                    login = subscription.login;
                    password = subscription.password;
                    subscription_id = subscription.subscription_id;
                    Config.Subscription_Id = subscription_id;

                    List<InternalServerEntry> newServers = new List<InternalServerEntry>();

                    InternalServerEntry optimalLocation = CreateOptimalLocationEntry(subscription);
                    if (optimalLocation != null)
                    {
                        newServers.Add(optimalLocation);
                    }

                    foreach (Node node in subscription.nodes)
                    {
                        InternalServerEntry entry = FindNode(node);
                        if (entry == null)
                        {
                            List<InternalServerEntry.Host> hosts = new List<InternalServerEntry.Host>();
                            foreach (Host host in node.hosts)
                            {
                                if (host == null)
                                {
                                    continue;
                                }

                                //if (!host.mode.Contains("ipsec"))
                                //{
                                //    continue;
                                //}

                                TimeSpan hostTimezone = new TimeSpan();

                                try
                                {
                                    if (!string.IsNullOrEmpty(host.timezone))
                                    {
                                        double hours = double.Parse(host.timezone);
                                        hostTimezone = TimeSpan.FromHours(hours);
                                    }
                                    else
                                    {
                                        hostTimezone = new TimeSpan();
                                    }
                                }
                                catch
                                {
                                    hostTimezone = new TimeSpan();
                                }

                                InternalServerEntry.Host internalHost = new InternalServerEntry.Host { Domen = host.host, Port = host.port, Timezone = hostTimezone };

                                hosts.Add(internalHost);
                            }

                            if (hosts.Count <= 0)
                            {
                                continue;
                            }

                            entry = new InternalServerEntry(node.country, node.country_code, node.display_name, node.city, node.priority, false, hosts, subscription.login, subscription.password);
                        }
                        else
                        {
                            entry.Update(node.country, node.display_name, node.priority, false, subscription.login, subscription.password);
                        }

                        newServers.Add(entry);
                    }

                    servers = newServers;
                }

                if (userChanged)
                {
                    if (syncConnector.IsConnected && syncConnector.CurrentEntry != null)
                    {
                        Logger.Log.Info("User changed. Begin disconnect");
                        syncConnector.BeginDisconnect(true);
                        return;
                    }
                }

                GlobalEvents.SubscriptionUpdated(this);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        private InternalServerEntry CreateOptimalLocationEntry(Subscription subscription)
        {
            if (syncConnector.CurrentEntry != null && syncConnector.CurrentEntry.CountryCode == "ol" && syncConnector.CurrentEntry.IsFree == isFree)
            {
                return syncConnector.CurrentEntry;
            }

            bool added = false;
            List<InternalServerEntry.Host> optimalHosts = new List<InternalServerEntry.Host>();
            foreach (Node node in subscription.nodes)
            {
                if (!node.free && isFree)
                {
                    continue;
                }

                if (!isFree)
                {
                    if (added)
                    {
                        break;
                    }
                }

                foreach (Host host in node.hosts)
                {
                    if (host == null)
                    {
                        continue;
                    }

                    TimeSpan hostTimezone = new TimeSpan();

                    try
                    {
                        if (!string.IsNullOrEmpty(host.timezone))
                        {
                            double hours = double.Parse(host.timezone);
                            hostTimezone = TimeSpan.FromHours(hours);
                        }
                        else
                        {
                            hostTimezone = new TimeSpan();
                        }
                    }
                    catch
                    {
                        hostTimezone = new TimeSpan();
                    }

                    InternalServerEntry.Host internalHost = new InternalServerEntry.Host { Domen = host.host, Port = host.port, Timezone = hostTimezone };

                    optimalHosts.Add(internalHost);
                    added = true;
                }
            }

            if (optimalHosts.Count > 0)
            {
                return new InternalServerEntry("Optimal location", "ol", "Optimal location", "Optimal location", 1, isFree, optimalHosts, subscription.login, subscription.password);
            }
            else
            {
                Logger.Log.Error("Optimal location hosts not found");
                return null;
            }
        }

        private bool FindEntry(InternalServerEntry entry, Subscription subscription)
        {
            bool res = false;

            foreach (Node node in subscription.nodes)
            {
                if (node.country_code == entry.CountryCode
                    && node.city == entry.City
                    && entry.EqualHosts(node.hosts)
                    && node.free == entry.IsFree)
                {
                    res = true;
                    break;
                }
            }

            return res;
        }

        private InternalServerEntry FindNode(Node node)
        {
            foreach (InternalServerEntry entry in servers)
            {
                if (node.country_code == entry.CountryCode
                    && node.city == entry.City
                    && entry.EqualHosts(node.hosts)
                    && (node.free == entry.IsFree || node.free == true))
                {
                    return entry;
                }
            }

            return null;
        }

        public InternalServerEntry FindAlternativeEntry(InternalServerEntry oldEntry)
        {
            foreach (InternalServerEntry entry in servers)
            {
                foreach (var host in entry.Hosts)
                {
                    if (host.Domen.Equals(oldEntry.CurrentDomen, StringComparison.CurrentCultureIgnoreCase)
                        && host.Port == oldEntry.CurrentPort)
                    {
                        Logger.Log.Info("Find alternative entry");
                        entry.CloneEntryStatus(oldEntry);
                        syncConnector.ReplaceConnectedEntry(entry);
                        return entry;
                    }
                }
            }

            return null;
        }

        public bool GetNotifications()
        {
            NotificationsResponse response = null;
            ErrorResponse errorResponse = null;
            bool result = false;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/notifications");
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;

                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("notifications");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    //string answ = sr.ReadLine();
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (NotificationsResponse)serializer.Deserialize(sr, typeof(NotificationsResponse));
                        result = true;
                    }
                    else if (webResponse.StatusCode == HttpStatusCode.Forbidden) //403
                    {
                        Config.Sid = "";
                        return PostActivation();
                    }
                    else
                    {
                        errorResponse = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));

                        LogError(errorResponse, webResponse.StatusCode);
                    }
                }

                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            if (result && response != null)
            {
                FillNotifications(response.notifications);
            }

            return result;
        }

        public bool GetSubscription()
        {
            AuthorizationResponse response = null;
            ErrorResponse errorResponse = null;
            bool result = false;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Config.ApiUrl + "/subscription");
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("X-API-Version", Config.ApiVersion);
                webRequest.Headers.Add("X-Session-ID", Config.Sid);
                webRequest.Headers.Add("X-Software", "win-app/" + Config.Version);
                webRequest.KeepAlive = false;

                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }

                if (webResponse.StatusCode == HttpStatusCode.BadGateway
                    || webResponse.StatusCode == HttpStatusCode.GatewayTimeout
                    || webResponse.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    SendTimeout("subscription");
                }

                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    //string answ = sr.ReadLine();
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (AuthorizationResponse)serializer.Deserialize(sr, typeof(AuthorizationResponse));
                        result = true;
                    }
                    else if (webResponse.StatusCode == HttpStatusCode.Forbidden) //403
                    {
                        Config.Sid = "";
                        return PostActivation();
                    }
                    else
                    {
                        errorResponse = (ErrorResponse)serializer.Deserialize(sr, typeof(ErrorResponse));

                        LogError(errorResponse, webResponse.StatusCode);
                    }
                }

                receiveStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            if (result && response != null)
            {
                email = response.email;

                if (response.subscription != null)
                {
                    FillSubscription(response.subscription);
                }               
            }

            return result;
        }

        public static bool GetGoogleAuthToken(string code, out string accessToken, out string idToken)
        {
            bool result = false;
            GoogleTokenResponse response = null;
            string request = string.Format("code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code",
                code,
                Config.GoogleClientId,
                Config.GoogleClientSecret,
                Config.GoogleRedirectUri
                );

            accessToken = string.Empty;
            idToken = string.Empty;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("https://www.googleapis.com/oauth2/v4/token");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.KeepAlive = false;
                Stream sendStream = webRequest.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(sendStream))
                {
                    sw.Write(request);
                }
                sendStream.Close();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }
                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (GoogleTokenResponse)serializer.Deserialize(sr, typeof(GoogleTokenResponse));
                        
                    }
                    else
                    {
                        Logger.Log.WarnFormat("BadResponse: status code: {0}, response: {1}", webResponse.StatusCode, response);
                    }
                }
                receiveStream.Close();
                webResponse.Close();

                if (response != null)
                {
                    accessToken = response.access_token;
                    idToken = response.id_token;

                    if (!string.IsNullOrEmpty(accessToken)
                        && !string.IsNullOrEmpty(idToken))
                    {
                        result = true;
                    }                    
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            return result;
        }

        public static bool GetGoogleUserEmail(string tokenId, out string email)
        {
            bool result = false;
            GoogleTokenInfoResponse response = null;

            email = string.Empty;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("https://www.googleapis.com/oauth2/v1/tokeninfo?id_token=" + tokenId);
                webRequest.Method = "GET";
                webRequest.KeepAlive = false;
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                }
                catch (WebException wex)
                {
                    webResponse = (HttpWebResponse)wex.Response;
                }
                Stream receiveStream = webResponse.GetResponseStream();
                using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        response = (GoogleTokenInfoResponse)serializer.Deserialize(sr, typeof(GoogleTokenInfoResponse));
                    }
                    else
                    {
                        GoogleErrorResponse errorResponse = (GoogleErrorResponse)serializer.Deserialize(sr, typeof(GoogleErrorResponse));
                        Logger.Log.WarnFormat("BadResponse: status code: {0}, response: {1}", webResponse.StatusCode, response);
                    }
                }
                receiveStream.Close();
                webResponse.Close();

                if (response != null && response.email != null)
                {
                    email = response.email;
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }

            return result;
        }

        private void FillNotifications(IList<Privatix.Core.Site.Notification> notifications)
        {
            lock (lockNotifications)
            {
                if (notifications == null || notifications.Count == 0)
                {
                    Logger.Log.Info("Notfications not found");
                    foreach (var notify in notificationsList)
                    {
                        notify.Close();
                    }
                    notificationsList.Clear();
                    return;
                }

                var deletedNotifications = new List<INotification>();

                //Remove deleted
                foreach (var notify in notificationsList)
                {
                    bool finded = false;
                    foreach (var siteNotify in notifications)
                    {
                        if (notify.Compare(siteNotify))
                        {
                            finded = true;
                            break;
                        }
                    }

                    if (!finded)
                    {
                        deletedNotifications.Add(notify);
                    }
                }
                foreach (var deleted in deletedNotifications)
                {
                    deleted.Close();
                    notificationsList.Remove(deleted);
                }

                //add new
                foreach (var siteNotify in notifications)
                {
                    bool finded = false;
                    foreach (var notify in notificationsList)
                    {
                        if (notify.Compare(siteNotify))
                        {
                            finded = true;
                            break;
                        }
                    }

                    if (!finded)
                    {
                        if (siteNotify.type.ToLower() == "banner")
                        {
                            BannerNotification bannerNotification = new BannerNotification(siteNotify.period, siteNotify.ttl, siteNotify.target, siteNotify.text, siteNotify.url, siteNotify.link);
                            notificationsList.Add(bannerNotification);
                        }
                        else if (siteNotify.type.ToLower() == "lock")
                        {
                            LockNotification lockNotification = new LockNotification(siteNotify.period, siteNotify.ttl, siteNotify.target, siteNotify.text, siteNotify.url, siteNotify.link);
                            notificationsList.Add(lockNotification);
                        }
                        else if (siteNotify.type.ToLower() == "page")
                        {
                            PageNotification pageNotification = new PageNotification(siteNotify.period, siteNotify.ttl, siteNotify.target, siteNotify.text, siteNotify.url, siteNotify.link);
                            notificationsList.Add(pageNotification);
                        }
                        else if (siteNotify.type.ToLower() == "disconnect")
                        {
                            DisconnectNotification disconnectNotification = new DisconnectNotification(siteNotify.ttl);
                            notificationsList.Add(disconnectNotification);
                        }
                        else
                        {
                            Logger.Log.ErrorFormat("Unknown notification type: {0}", siteNotify.type);
                        }
                    }
                }

            }
        }

        public LockNotification GetLock()
        {
            lock (lockNotifications)
            {
                return notificationsList.FirstOrDefault(n => n.NotifyType == NotificationType.Lock) as LockNotification;
            }
        }

        public bool CheckNotifyTarget(string target)
        {
            if (string.Equals(target, "view:bottom", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (string.Equals(target, "state:disconnected", StringComparison.OrdinalIgnoreCase))
            {
                return !syncConnector.IsConnected;
            }
            else if (string.Equals(target, "state:free_connected", StringComparison.OrdinalIgnoreCase))
            {
                return syncConnector.IsConnected && isFree;
            }

            return false;
        }        

        private void LogError(ErrorResponse errorResponse, HttpStatusCode statusCode)
        {
            try
            {
                string errorMessage;

                if (errorResponse == null)
                {
                    errorMessage = ((int)statusCode).ToString();
                }

                else
                {
                    if (errorResponse.status == null)
                        errorResponse.status = "";
                    if (errorResponse.error == null)
                        errorResponse.error = "";
                    if (errorResponse.message == null)
                        errorResponse.message = "";

                    errorMessage = string.Format("{0} Status: {1}; Error: {2} {3}; Message: {4}", (int)statusCode, errorResponse.status, errorResponse.error_code, errorResponse.error, errorResponse.message);
                }

                throw new Exception(errorMessage);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                Metadata metadata = new Metadata();
                metadata.AddToTab("Resources", "Full stacktrace", Environment.StackTrace);
                WPFClient.Notify(ex, Bugsnag.Severity.Info, metadata);
            }
        }

        public void BeginUpdateSession()
        {
            updateSessionTimer = new Timer(UpdateSession, null, Config.ServerListUpdatePeriod * 1000, Timeout.Infinite);
        }

        public void UpdateSessionImmediate()
        {
            if (sessionUpdating)
            {
                Logger.Log.Warn("Session already updating...");
                return;
            }

            if (updateSessionTimer != null)
            {
                updateSessionTimer.Change(100, Timeout.Infinite);
            }
        }

        private void UpdateSession(object stateInfo)
        {
            if (SyncConnector.Instance.Status == InternalStatus.Connecting
                || SyncConnector.Instance.Status == InternalStatus.Disconnecting)
            {
                updateSessionTimer.Change(Config.ServerListUpdatePeriod * 1000, Timeout.Infinite);
                return;
            }

            Logger.Log.Info("Start Update Session");
            sessionUpdating = true;
            bool result = GetSession();
            sessionUpdating = false;
            Logger.Log.InfoFormat("End Update Session, result = {0}", result.ToString());

            if (result)
            {
                getSessionAttemps = 0;
                updateSessionTimer.Change(Config.ServerListUpdatePeriod * 1000, Timeout.Infinite);
            }
            else
            {
                getSessionAttemps++;
                if (getSessionAttemps <= 5)
                {
                    updateSessionTimer.Change(1 * 1000, Timeout.Infinite);
                }
                else
                {
                    updateSessionTimer.Change(20 * 1000, Timeout.Infinite);
                }
            }
        }

        private void SendTimeout(string apiMethod)
        {
            try
            {
                string label = string.Format("{0}_{1}_{2}", originalcountry.ToUpper(), currentCountry.ToUpper(), apiMethod.ToUpper());
                GoogleAnalyticsApi.TrackEvent("timeout", "win", label);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        public void SendErrorTrace(bool isTimeout, string error, string errorTrace, string connectionNode)
        {
            //Send error trace only one in 1 hour
            if ((DateTime.Now - lastErrorPost).TotalMinutes < 60)
            {
                return;
            }
            lastErrorPost = DateTime.Now;


            ThreadPool.QueueUserWorkItem(o =>
                {
                    

                    int datetime = (int)((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds);
                    string type = isTimeout ? "timeout" : "connect";

                    string output = "";
                    if (RunProcessAndGetOutput("ipconfig.exe", "/all", out output))
                    {
                        errorTrace += "\n\n ipconfig /all\n" + output;
                    }
                    if (RunProcessAndGetOutput("route.exe", "print", out output))
                    {
                        errorTrace += "\n\n route print\n" + output;
                    }

                    int attempts = 10;
                    while (attempts > 0)
                    {
                        if (PostError(type, datetime, error, errorTrace, connectionNode))
                        {
                            break;
                        }

                        attempts--;
                        Thread.Sleep(10000);
                    }
                }
                );
        }

        private bool RunProcessAndGetOutput(string filename, string args, out string output)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = filename;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();

                output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.Log.Warn(ex.Message, ex);
                output = "";
                return false;
            }            
        }

        private void GlobalEvents_OnEnded(object sender, ServerEntryEventArgs args)
        {
            ResetIpInfo();

            if (!syncConnector.ChangingServer &&
                syncConnector.Status == InternalStatus.Disconnected)
            {
                UpdateSessionImmediate();
            }
        }

        private void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            ResetIpInfo();

            UpdateSessionImmediate();
        }

        private void ResetIpInfo()
        {
            currentIp = "Checking IP...";
            GlobalEvents.IpInfoUpdated(this);
        }

        #region Reserved Api Host

        public bool GetReservedApiHost()
        {
            try
            {
                //Load
                PrivatixWebClient webClient = new PrivatixWebClient();
                string content = webClient.DownloadString(Config.ReservedSiteSourceUrl);
                if (string.IsNullOrEmpty(content))
                {
                    return false;
                }

                //Parse
                string reservedHost = ParseReservedApiHost(content);
                if (string.IsNullOrEmpty(reservedHost))
                {
                    return false;
                }

                //Compare
                if (reservedHost.Equals(Config.ReservedApiHost, StringComparison.CurrentCultureIgnoreCase))
                {
                    Logger.Log.Info("Reserved host already used: " + reservedHost);
                    return false;
                }

                //Update
                Config.UpdateReservedApiHost(reservedHost);
                //TODO: ga event
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }

            return true;
        }

        private string ParseReservedApiHost(string content)
        {
            try
            {
                string[] values = content.Split(new char[] { '|' });
                if (values.Length != 2)
                {
                    Logger.Log.Error("Invalid content. Parse error");
                    return null;
                }

                string format = values[0].Trim();
                string data = values[1].TrimEnd(new char[] {' ', '\t', '\n', '\r'});

                if (string.IsNullOrEmpty(format)
                    || string.IsNullOrEmpty(data))
                {
                    Logger.Log.Error("Invalid content. Parse error");
                    return null;
                }

                if (format.Equals("PLAIN", StringComparison.CurrentCultureIgnoreCase))
                {
                    return data;
                }
                else if (format.Equals("ENCRYPTED", StringComparison.CurrentCultureIgnoreCase))
                {
                    return DecryptReservedApiHost(data);
                }
                else
                {
                    Logger.Log.Error("Invalid format: " + format);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return null;
            }            
        }

        private string DecryptReservedApiHost(string cryptedHost)
        {
            try
            {
                string result = null;
                byte[] cryptedData = Convert.FromBase64String(cryptedHost);

                using (RijndaelManaged rijndael = new RijndaelManaged())
                {
                    rijndael.IV = StringToByteArray(Config.ReservedHostEncryptIV);
                    rijndael.Key = StringToByteArray(Config.ReservedHostEncryptKey);
                    rijndael.Mode = CipherMode.CBC;
                    rijndael.BlockSize = 128;

                    ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
                    using (MemoryStream msDecrypt = new MemoryStream(cryptedData))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Decrypt reserved host error", ex);
                return null;
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        #endregion

        #endregion

    }        
}
