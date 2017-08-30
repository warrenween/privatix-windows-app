using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.Win32;
using Privatix.Core.Delegates;


namespace Privatix.Core
{
    public class SpeedTester
    {
        private enum ActionType
        {
            CDN,
            Node
        }

        private WebClient webClient = new WebClient();
        private string originalCountry = "";
        private Dictionary<string, int> lastTests = new Dictionary<string, int>();
        private bool isWorking = false;
        private string currentLabel = "";
        private ActionType currentAction = ActionType.CDN;
        private bool currentIsOpenVpn = false;
        private string currentTempFileName = "";
        private DateTime currentStartTime = DateTime.MinValue;
        private object lockWorking = new object();
        //private NetworkInterface currentInterface = null;
        //private long currentBytesReceived = 0;

        public SpeedTester(string country)
        {
            originalCountry = country;

            LoadTestsResults();

            webClient.DownloadFileCompleted += webClient_DownloadFileCompleted;
            //webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;

            GlobalEvents.OnConnected += GlobalEvents_OnConnected;
            GlobalEvents.OnDisconnected += GlobalEvents_OnDisconnected;

            string label = originalCountry.ToLower();
            if (!lastTests.ContainsKey(label))
            {
                StartTest(Config.SpeedTestCdnUrl, label, ActionType.CDN, false);
            }
        }

        //private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        //{
        //    if (currentStartTime == DateTime.MinValue)
        //    {
        //        currentStartTime = DateTime.Now;

        //        try
        //        {
        //            var routes = RouteTable.Read();
        //            var defRoute = routes.OrderBy(r => r.Metric).FirstOrDefault(r => r.Destination.Equals(IPAddress.Any) && r.Mask.Equals(IPAddress.Any) && r.IfIndex != NetworkInterface.LoopbackInterfaceIndex);
        //            if (defRoute.Gateway.Equals(IPAddress.Any))
        //            {
        //                return;
        //            }

        //            var ifsList = NetworkInterface.GetAllNetworkInterfaces();
        //            currentInterface = ifsList.FirstOrDefault(i => i.GetIPProperties().GetIPv4Properties().Index == defRoute.IfIndex);
        //            if (currentInterface == null)
        //            {
        //                return;
        //            }
        //            currentBytesReceived = currentInterface.GetIPv4Statistics().BytesReceived;
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Log.Error(ex.Message, ex);
        //            currentInterface = null;
        //            currentBytesReceived = 0;
        //        }
        //    }
        //}

        private void webClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            lock (lockWorking)
            {
                try
                {
                    if (!e.Cancelled && e.Error == null && currentStartTime != DateTime.MinValue)
                    {
                        double downloadTimeSec = (DateTime.Now - currentStartTime).TotalSeconds;
                        double downloadSizeMbit = 0;
                        FileInfo fileInfo = new FileInfo(currentTempFileName);
                        downloadSizeMbit = (double)fileInfo.Length * 8 / (1024 * 1024) ;

                        int speedMbitSec = (int)(downloadSizeMbit / downloadTimeSec + 0.5);
                        GoogleAnalyticsApi.TrackEvent(currentIsOpenVpn ? "speed_openvpn" : "speed", currentAction == ActionType.CDN ? "cdn" : "node", currentLabel, speedMbitSec);
                        SaveTestResult(currentLabel, speedMbitSec, currentIsOpenVpn);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
                finally
                {
                    try
                    {
                        File.Delete(currentTempFileName);
                    }
                    catch { } //if cant delete or file not exist - do nothing
                    currentTempFileName = "";
                    currentLabel = "";
                    currentStartTime = DateTime.MinValue;
                    isWorking = false;
                }
            }
        }

        private void GlobalEvents_OnConnected(object sender, ServerEntryEventArgs args)
        {
            if (isWorking)
            {
                return;
            }

            try
            {
                string label = (originalCountry + "_" + args.Entry.CountryCode).ToLower();
                bool isOpenVpn = SyncConnector.Instance.CurrentProtocol != VpnProtocol.IPsec;
                if (isOpenVpn)
                {
                    if (lastTests.ContainsKey(label + "_ovpn"))
                    {
                        return;
                    }
                }
                else
                {
                    if (lastTests.ContainsKey(label))
                    {
                        return;
                    }
                }

                int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string fileUrl = string.Format(Config.SpeedTestNodeUrlTemplate, args.Entry.CurrentDomen, unixTimestamp);

                ThreadPool.QueueUserWorkItem(o =>
                {
                    Thread.Sleep(5000);
                    StartTest(fileUrl, label, ActionType.Node, isOpenVpn);
                }
                );
                
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        private void GlobalEvents_OnDisconnected(object sender, ServerEntryEventArgs args)
        {
            if (isWorking)
            {
                webClient.CancelAsync();
            }
        }

        private void LoadTestsResults()
        {
            try
            {
                lastTests.Clear();
                RegistryKey speedTestKey = Registry.CurrentUser.OpenSubKey(Config.RegRoot + "SpeedTest");
                if (speedTestKey == null)
                {
                    return;
                }
                var valuesList = speedTestKey.GetValueNames();
                foreach (var valueName in valuesList)
                {
                    object val = speedTestKey.GetValue(valueName, 0);
                    if (val == null || !(val is int))
                    {
                        continue;
                    }
                    int intVal = (int)val;
                    lastTests.Add(valueName, intVal);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        private void SaveTestResult(string valueName, int value, bool isOpenVpn)
        {
            if (string.IsNullOrEmpty(valueName))
            {
                return;
            }

            if (isOpenVpn)
            {
                valueName += "_ovpn";
            }

            try
            {
                lastTests[valueName] = value;
                Registry.SetValue(Config.RegRootFull + "SpeedTest", valueName, value, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
        }

        private void StartTest(string url, string label, ActionType actionType, bool isOpenVpn)
        {
            lock (lockWorking)
            {
                try
                {
                    currentTempFileName = Path.GetTempFileName();
                    webClient.DownloadFileAsync(new Uri(url), currentTempFileName);

                    currentLabel = label;
                    currentAction = actionType;
                    currentIsOpenVpn = isOpenVpn;
                    //currentInterface = null;
                    //currentBytesReceived = 0;
                    isWorking = true;

                    currentStartTime = DateTime.Now;

                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            }            
        }
    }
}
