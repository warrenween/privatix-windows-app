using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;


namespace Privatix.Core
{
    public static class GoogleAnalyticsApi
    {
        public static void TrackEvent(string category, string action, string label = "", int? value = null)
        {
            ThreadPool.QueueUserWorkItem(o =>
                {
                    Track(HitType.@event, category, action, label, value);
                }
            );
        }

        public static void TrackEventAndWait(string category, string action, string label = "", int? value = null)
        {
            Track(HitType.@event, category, action, label, value);
        }

        public static void TrackPageview(string category, string action, string label = "", int? value = null)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Track(HitType.@pageview, category, action, label, value);
            }
            );
        }

        private static void Track(HitType type, string category, string action, string label,
                                  int? value = null)
        {
            if (string.IsNullOrEmpty(category)) throw new ArgumentNullException("category");
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException("action");

            

            // the request body we want to send
            var postData = new Dictionary<string, string>
                           {
                               { "v", "1" },
                               { "tid", Config.GoogleAnalyticsTrackingCode },
                               { "cid", "555" },
                               { "t", type.ToString() },
                               { "ec", category },
                               { "ea", action },
                           };
            if (!string.IsNullOrEmpty(label))
            {
                postData.Add("el", label);
            }
            if (value.HasValue)
            {
                postData.Add("ev", value.ToString());
            }

            var postDataString = postData
                .Aggregate("", (data, next) => string.Format("{0}&{1}={2}", data, next.Key,
                                                             HttpUtility.UrlEncode(next.Value)))
                .Trim('&');

            var request = (HttpWebRequest)WebRequest.Create("https://www.google-analytics.com/collect?" + postDataString);
            request.Method = "GET";
            //request.ContentLength = Encoding.UTF8.GetByteCount(postDataString);
            request.KeepAlive = false;

            //// write the request body to the request
            //using (var writer = new StreamWriter(request.GetRequestStream()))
            //{
            //    writer.Write(postDataString);
            //}

            try
            {
                var webResponse = (HttpWebResponse)request.GetResponse();
                if (webResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpException((int)webResponse.StatusCode,
                                            "Google Analytics tracking did not return OK 200");
                }
                //Stream receiveStream = webResponse.GetResponseStream();
                //using (StreamReader sr = new StreamReader(receiveStream, Encoding.UTF8))
                //{
                //    string answ = sr.ReadToEnd();
                //    answ = answ;
                //}
                webResponse.Close();
            }
            catch 
            {
                try
                {
                    Thread.Sleep(10000);
                    request = (HttpWebRequest)WebRequest.Create("https://www.google-analytics.com/collect?" + postDataString);
                    request.Method = "GET";
                    request.KeepAlive = false;
                    var webResponse = (HttpWebResponse)request.GetResponse();
                    webResponse.Close();
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex.Message, ex);
                }
            }
        }

        private enum HitType
        {
            @event,
            @pageview,
        }
    }
}
