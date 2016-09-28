// <copyright company="SIX Networks GmbH" file="ServerQueryQueue.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery.Extensions;
using GameServerQuery.Parsers;

namespace GameServerQuery
{
    public class ServerQueryQueue : IServerQueryQueue
    {
        const int StartPort = 50000;
        private static readonly SourceQueryParser sourceQueryParser = new SourceQueryParser();
        readonly int _maxConnections;
        readonly bool _useRangedEndPoints;
        ConcurrentQueue<IPEndPoint> _endpoints;
        int _lastPort;

        public ServerQueryQueue(bool useRangedEndpoints = true, int maxConnections = 60) {
            State = new ServerQueryOverallState();
            _useRangedEndPoints = useRangedEndpoints;
            _maxConnections = maxConnections;
        }

        public IServerQueryOverallState State { get; private set; }

        public Task SyncAsync(IServer[] objects) {
            return Sync(objects, default(CancellationToken));
        }

        public virtual Task SyncAsync(IServer[] objects, CancellationToken token) {
            return Sync(objects, token);
        }

        Task Sync(ICollection<IServer> objects, CancellationToken token) {
            State = new ServerQueryOverallState {Maximum = objects.Count, UnProcessed = objects.Count};

            return Enumerable.Empty<ServerQueryState>().SimpleRunningQueueAsync(1,
                blockingCollection => SyncServers(objects, token, state => {
                    if (state.Status != Status.SuccessParsing)
                        return;
                    blockingCollection.Add(state, token);
                }),
                x => {
                    try {
                        x.UpdateServer();
                    } catch (Exception ex) {
                        x.Exception = ex;
                        State.IncrementProcessed();
                    }
                });
        }

        Task SyncServers(IEnumerable<IServer> objects, CancellationToken token = default(CancellationToken),
            Action<ServerQueryState> callback = null) {
            if (_useRangedEndPoints)
                SetupEndPoints();
            return TrySyncServers(objects, token, callback);
        }

        void SetupEndPoints() {
            var p = StartPort;
            _endpoints =
                new ConcurrentQueue<IPEndPoint>(
                    Enumerable.Range(1, _maxConnections)
                        .Select(x => new IPEndPoint(IPAddress.Any, p += 1)));
            _lastPort = p;
        }

        async Task TrySyncServers(IEnumerable<IServer> objects, CancellationToken token,
            Action<ServerQueryState> callback) {
            try {
                await
                    objects.StartConcurrentTaskQueue(token, async server => {
                            var state = await SyncServer(server).ConfigureAwait(false);
                            if ((callback != null) && (state != null))
                                callback(state);
                        },
                        () => _maxConnections).ConfigureAwait(false);
            } finally {
                State.Progress = State.Maximum;
            }
        }

        async Task<ServerQueryState> SyncServer(IServer server) {
            IPEndPoint endPoint = null;
            while (true) {
                var portInUse = false;
                try {
                    if (endPoint == null)
                        endPoint = _useRangedEndPoints ? await GetEndPointFromQueue().ConfigureAwait(false) : null;
                    var state = await UpdateServer(server, endPoint).ConfigureAwait(false);
                    if (state.Status == Status.Cancelled)
                        State.IncrementCancelled();
                    return state;
                } catch (SocketException e) {
                    if (e.SocketErrorCode == SocketError.AddressAlreadyInUse) {
                        // replace used bad port with new port and retry, repeat until running out of ports
                        portInUse = true;
                        endPoint = GetNextEndPoint();
                    } else
                        return null;
                } catch (OperationCanceledException e) {
                    return null;
                } catch (Exception e) {
                    return null;
                } finally {
                    if (_useRangedEndPoints && (endPoint != null) && !portInUse)
                        ReleaseEndPoint(endPoint);
                }
            }
        }

        async Task<ServerQueryState> UpdateServer(IServer server, IPEndPoint endPoint) {
            // TODO: The queue should not be just for Source ;-)
            using (var state = new ServerQueryState(server, sourceQueryParser, endPoint)) {
                await new SourceServerQuery(state, "arma3").UpdateAsync().ConfigureAwait(false);
                return state;
            }
        }

        IPEndPoint GetNextEndPoint() {
            var port = Interlocked.Increment(ref _lastPort);
            if (port > IPEndPoint.MaxPort)
                throw new RanOutOfPortsException("Reached MaxPort");
            return new IPEndPoint(IPAddress.Any, port);
        }

        async Task<IPEndPoint> GetEndPointFromQueue() {
            IPEndPoint ep;
            var retry = 0;
            while (!_endpoints.TryDequeue(out ep)) {
                if (retry++ > 10) {
                    ep = null;
                    break;
                }
                await Task.Delay(150).ConfigureAwait(false);
            }
            if (ep == null)
                throw new RanOutOfPortsException("Endpoint not available from Queue");
            return ep;
        }

        void ReleaseEndPoint(IPEndPoint endPoint) {
            _endpoints.Enqueue(endPoint);
        }
    }

    class RanOutOfPortsException : Exception
    {
        public RanOutOfPortsException(string message) : base(message) {}
    }
}