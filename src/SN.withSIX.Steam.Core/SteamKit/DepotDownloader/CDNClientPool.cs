using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace SN.withSIX.Steam.Core.SteamKit.DepotDownloader
{
    /// <summary>
    ///     CDNClientPool provides a pool of CDNClients to CDN endpoints
    ///     CDNClients that get re-used will be initialized for the correct depots
    /// </summary>
    class CDNClientPool : IDisposable
    {
        private const int ServerEndpointMinimumSize = 8;
        private readonly ConcurrentDictionary<CDNClient, Tuple<uint, CDNClient.Server>> activeClientAuthed;

        private readonly ConcurrentBag<CDNClient> activeClientPool;
        private readonly BlockingCollection<CDNClient.Server> availableServerEndpoints;

        private readonly AutoResetEvent populatePoolEvent;

        private readonly Steam3Session _steamSession;
        private readonly CancellationTokenSource _monitorCts;

        public CDNClientPool(Steam3Session steamSession) {
            _steamSession = steamSession;

            activeClientPool = new ConcurrentBag<CDNClient>();
            activeClientAuthed = new ConcurrentDictionary<CDNClient, Tuple<uint, CDNClient.Server>>();
            availableServerEndpoints = new BlockingCollection<CDNClient.Server>();

            populatePoolEvent = new AutoResetEvent(true);
            _monitorCts = new CancellationTokenSource();

            Task.Factory.StartNew(ConnectionPoolMonitor, _monitorCts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Dispose() {
            _monitorCts.Cancel();
            _monitorCts.Dispose();
        }

        private IReadOnlyCollection<CDNClient.Server> FetchBootstrapServerList() {
            var bootstrap = new CDNClient(_steamSession.steamClient);

            while (true) {
                try {
                    var cdnServers = bootstrap.FetchServerList(cellId: _steamSession.CellID);
                    if (cdnServers != null)
                        return cdnServers.AsReadOnly();
                } catch (Exception ex) {
                    ContentDownloader.Log($"Failed to retrieve content server list: {ex.Message}");
                }
            }
        }

        private async Task ConnectionPoolMonitor() {
            while (true) {
                _monitorCts.Token.ThrowIfCancellationRequested();
                populatePoolEvent.WaitOne(TimeSpan.FromSeconds(1));

                // peek ahead into steam session to see if we have servers
                if (availableServerEndpoints.Count < ServerEndpointMinimumSize &&
                    _steamSession.steamClient.IsConnected &&
                    _steamSession.steamClient.GetServersOfType(EServerType.CS).Count > 0) {
                    var servers = FetchBootstrapServerList();

                    var weightedCdnServers = servers.Select(x => {
                        var penalty = 0;
                        ConfigStore.TheConfig.ContentServerPenalty.TryGetValue(x.Host, out penalty);

                        return Tuple.Create(x, penalty);
                    }).OrderBy(x => x.Item2).ThenBy(x => x.Item1.WeightedLoad);

                    foreach (var endpoint in weightedCdnServers) {
                        for (var i = 0; i < endpoint.Item1.NumEntries; i++)
                            availableServerEndpoints.Add(endpoint.Item1);
                    }
                }
            }
        }

        private void ReleaseConnection(CDNClient client) {
            Tuple<uint, CDNClient.Server> authData;
            activeClientAuthed.TryRemove(client, out authData);
        }

        private CDNClient BuildConnection(uint depotId, byte[] depotKey, CDNClient.Server serverSeed,
            CancellationToken token) {
            CDNClient.Server server = null;
            CDNClient client = null;

            while (client == null) {
                // if we want to re-initialize a specific content server, try that one first
                if (serverSeed != null) {
                    server = serverSeed;
                    serverSeed = null;
                } else {
                    if (availableServerEndpoints.Count < ServerEndpointMinimumSize)
                        populatePoolEvent.Set();

                    server = availableServerEndpoints.Take(token);
                }

                client = new CDNClient(_steamSession.steamClient, _steamSession.AppTickets[depotId]);

                string cdnAuthToken = null;

                if (server.Type == "CDN") {
                    _steamSession.RequestCDNAuthToken(depotId, server.Host);
                    cdnAuthToken = _steamSession.CDNAuthTokens[Tuple.Create(depotId, server.Host)].Token;
                }

                try {
                    client.Connect(server);
                    client.AuthenticateDepot(depotId, depotKey, cdnAuthToken);
                } catch (Exception ex) {
                    client = null;

                    ContentDownloader.Log($"Failed to connect to content server {server}: {ex.Message}");

                    var penalty = 0;
                    ConfigStore.TheConfig.ContentServerPenalty.TryGetValue(server.Host, out penalty);
                    ConfigStore.TheConfig.ContentServerPenalty[server.Host] = penalty + 1;
                }
            }

            ContentDownloader.Log($"Initialized connection to content server {server} with depot id {depotId}");

            activeClientAuthed[client] = Tuple.Create(depotId, server);
            return client;
        }

        private bool ReauthConnection(CDNClient client, CDNClient.Server server, uint depotId, byte[] depotKey) {
            DebugLog.Assert(server.Type == "CDN" || _steamSession.AppTickets[depotId] == null, "CDNClientPool",
                "Re-authing a CDN or anonymous connection");

            string cdnAuthToken = null;

            if (server.Type == "CDN") {
                _steamSession.RequestCDNAuthToken(depotId, server.Host);
                cdnAuthToken = _steamSession.CDNAuthTokens[Tuple.Create(depotId, server.Host)].Token;
            }

            try {
                client.AuthenticateDepot(depotId, depotKey, cdnAuthToken);
                activeClientAuthed[client] = Tuple.Create(depotId, server);
                return true;
            } catch (Exception ex) {
                ContentDownloader.Log($"Failed to reauth to content server {server}: {ex.Message}");
            }

            return false;
        }

        public CDNClient GetConnectionForDepot(uint depotId, byte[] depotKey, CancellationToken token) {
            CDNClient client = null;

            Tuple<uint, CDNClient.Server> authData;

            activeClientPool.TryTake(out client);

            // if we couldn't find a connection, make one now
            if (client == null)
                client = BuildConnection(depotId, depotKey, null, token);

            // if we couldn't find the authorization data or it's not authed to this depotid, re-initialize
            if (!activeClientAuthed.TryGetValue(client, out authData) || authData.Item1 != depotId) {
                if (authData.Item2.Type == "CDN" && ReauthConnection(client, authData.Item2, depotId, depotKey)) {
                    ContentDownloader.Log(
                        $"Re-authed CDN connection to content server {authData.Item2} from {authData.Item1} to {depotId}");
                } else if (authData.Item2.Type == "CS" && _steamSession.AppTickets[depotId] == null &&
                           ReauthConnection(client, authData.Item2, depotId, depotKey)) {
                    ContentDownloader.Log(
                        $"Re-authed anonymous connection to content server {authData.Item2} from {authData.Item1} to {depotId}");
                } else {
                    ReleaseConnection(client);
                    client = BuildConnection(depotId, depotKey, authData.Item2, token);
                }
            }

            return client;
        }

        public void ReturnConnection(CDNClient client) {
            if (client == null)
                return;

            activeClientPool.Add(client);
        }

        public void ReturnBrokenConnection(CDNClient client) {
            if (client == null)
                return;

            ReleaseConnection(client);
        }
    }
}