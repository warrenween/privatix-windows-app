using System;
using System.Linq;
using System.Collections.Generic;


namespace Privatix.Core
{
    public class Reconnector
    {
        private struct Connection
        {
            public InternalServerEntry Entry;
            public int HostIndex;
        }


        private List<Connection> connections;
        private int reconnectIndex;


        public Reconnector(InternalServerEntry entry)
        {
            connections = new List<Connection>();

            SiteConnector site = SiteConnector.Instance;

            if (site != null)
            {
                var enries = site.Servers.Where(e => (e.CountryCode.Equals(entry.CountryCode, StringComparison.CurrentCultureIgnoreCase)
                                                && e.City.Equals(entry.City, StringComparison.CurrentCultureIgnoreCase))
                                                && (e.IsFree || !site.IsFree));

                foreach (InternalServerEntry e in enries)
                {
                    for (int index = 0; index < e.Hosts.Count; index++)
                    {
                        if (!IsConnectionInList(e, index))
                        {
                            connections.Add(new Connection { Entry = e, HostIndex = index });
                        }
                    }
                }

            }

            Logger.Log.Info("Created Reconnector with " + connections.Count + " connections");

            reconnectIndex = 0;
        }

        private bool IsConnectionInList(InternalServerEntry entry, int index)
        {
            try
            {
                foreach (var conn in connections)
                {
                    if (string.Equals(conn.Entry.Hosts[conn.HostIndex].Domen, entry.Hosts[index].Domen, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
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

        public bool TryConnect()
        {
            try
            {
                if (reconnectIndex >= connections.Count)
                {
                    return false;
                }

                Logger.Log.Info("Reconnector try connect to index " + reconnectIndex);
                Connection conn = connections[reconnectIndex];

                if (conn.Entry.ConnectStatus != InternalStatus.Disconnected)
                {
                    return true;
                }

                SyncConnector.Instance.BeginConnect(conn.Entry, conn.HostIndex);
                reconnectIndex++;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }
        }
    }
}
