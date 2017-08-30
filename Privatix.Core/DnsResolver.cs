using System;
using System.Net;
using Heijden.DNS;
using TransportType = Heijden.DNS.TransportType;

namespace Privatix.Core
{
    public static class DnsResolver
    {
        private static Resolver _resolver;
        public const string DefaultDnsServer = "8.8.8.8";

        public static IPAddress[] ResolveHost(string host)
        {
            if (Guard.Instance.IsGuardEnabled)
            {
                if (!Guard.Instance.AddDefaultDns())
                {
                    Logger.Log.Error("Could not add route for default DNS");
                    //return null;
                }
            }

            try
            {
                if (_resolver == null)
                {
                    _resolver = new Heijden.DNS.Resolver
                    {
                        Retries = 3,
                        Recursion = true,
                        TransportType = TransportType.Udp,
                        TimeOut = 3,
                        UseCache = false
                    };
                }
                _resolver.DnsServer = DefaultDnsServer;

                try
                {
                    var lst = _resolver.GetHostAddresses(host);
                    if (lst == null || lst.Length == 0)
                        return null;
                    return lst;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("Error on DNS " + _resolver.DnsServer + " : " + ex.Message, ex);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
            }
            finally
            {
                if (Guard.Instance.IsGuardEnabled)
                {
                    Guard.Instance.DeleteDefaultDns();
                }
            }

            return null;
        }
    }
}
