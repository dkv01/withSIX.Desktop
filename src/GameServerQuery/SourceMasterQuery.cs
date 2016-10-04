// <copyright company="SIX Networks GmbH" file="SourceMasterQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery.Extensions;
using GameServerQuery.Parsers;

namespace GameServerQuery
{
    // https://developer.valvesoftware.com/wiki/Master_Server_Query_Protocol
    public class SourceMasterQuery : ServerQueryBase, IMasterServerQuery
    {
        public enum Region
        {
            UsEast = 0x00,
            UsWest = 0x01,
            SouthAmerica = 0x02,
            Europe = 0x03,
            Asia = 0x04,
            Australia = 0x05,
            MiddleEast = 0x06,
            Africa = 0x07,
            All = 0xFF
        }

        private readonly List<Tuple<string, string>> _filter;
        readonly Region _region;

        public SourceMasterQuery(List<Tuple<string, string>> filter, Region region = Region.All) {
            _filter = filter;
            _region = region;
        }

        string Filter => string.Join("", _filter.Select(x => string.Format(@"\{0}\{1}", x.Item1, x.Item2)));

        public virtual async Task<IEnumerable<ServerQueryResult>> GetParsedServers(CancellationToken cancelToken,
            bool forceLocal = false, int limit = 0) {
            var serversResult = await RetrieveAsync(cancelToken, limit).ConfigureAwait(false);
            return serversResult.Select(CreateServerDictionary);
        }

        public event EventHandler<ServerPageArgs> ServerPageReceived;

        void Raise(ServerPageArgs args) => ServerPageReceived?.Invoke(this, args);

        internal async Task<List<IPEndPoint>> RetrieveAsync(CancellationToken cancelToken, int limit,
            IPEndPoint remote = null, int tried = -1) {
            var cur = -1;
            var servers = new List<IPEndPoint>();
            if (remote == null) {
                var hosts = await Dns.GetHostAddressesAsync("hl2master.steampowered.com").ConfigureAwait(false);
                if (tried == -1)
                    cur = new Random().Next(hosts.Length);
                else
                    cur = (tried + 1)%hosts.Length;
                var host = hosts[cur];
                const int port = 27011;

                remote = new IPEndPoint(host, port); //TODO: alternate ip and ports availability
            }
            var e = new IPEndPoint(IPAddress.Any, 0);
            var udpClient = new UdpClient(e) {
                Client = {ReceiveTimeout = DefaultReceiveTimeout, SendTimeout = DefaultSendTimeout}
            };

            var i = 0;
            const int maxIterations = 30;
            string lastSeed;
            var seed = "0.0.0.0:0";
            do {
                cancelToken.ThrowIfCancellationRequested();
                lastSeed = seed;
                var msg = BuildMessage(seed);

                if (await udpClient.SendAsync(msg, msg.Length, remote).ConfigureAwait(false) != msg.Length)
                    throw new Exception("Send failed.");
                byte[] response = null;
                var timedOut = false;
                try {
                    response =
                        (await
                                udpClient.ReceiveWithTimeoutAfter(DefaultReceiveTimeout, cancelToken).ConfigureAwait(false))
                            .Buffer;
                } catch (TimeoutException) {
                    timedOut = true;
                    if ((cur == -1) || (tried != -1) || (i != 0)) {
                        throw new TimeoutException(string.Format("Received timeout on try: {0} packet: {1}",
                            tried == -1 ? 1 : 2, i));
                    }
                }
                //only retries when remote was not passed in, on the first try, and when the first packet was never received
                if (timedOut)
                    return await RetrieveAsync(cancelToken, limit, null, cur).ConfigureAwait(false);
                seed = ParseResponse(servers, response, limit);
                if (seed == null)
                    throw new Exception("Bad packet recieved.");
                i++;
            } while ((i < maxIterations) && !seed.Equals("0.0.0.0:0") && !seed.Equals(lastSeed));
            return servers;
        }

        byte[] BuildMessage(string seed) {
            byte[] m1 = {0x31, (byte) _region};
            var m2 = Encoding.ASCII.GetBytes(seed);
            byte[] m3 = {0x00};
            var m4 = Encoding.ASCII.GetBytes(Filter);
            byte[] m5 = {0x00};
            var mF = m1.Concat(m2).Concat(m3).Concat(m4).Concat(m5).ToArray();
            return mF;
        }

        string ParseResponse(ICollection<IPEndPoint> servers, byte[] reply, int limit) {
            var seed = string.Empty;
            var pos = 0;
            byte[] header = {0xFF, 0xFF, 0xFF, 0xFF, 0x66, 0x0A};
            if (!header.All(t => reply[pos++] == t))
                return null;
            var page = new List<IPEndPoint>();
            while (pos < reply.Length) {
                var ip = $"{reply[pos++]}.{reply[pos++]}.{reply[pos++]}.{reply[pos++]}";
                byte[] b = {reply[pos + 1], reply[pos]};
                var port = BitConverter.ToUInt16(b, 0);
                pos += 2;

                seed = ip + ":" + port;
                if (seed.Equals("0.0.0.0:0"))
                    break;
                var ep = seed.ToIPEndPoint();
                if (servers.Contains(ep))
                    continue;
                servers.Add(ep);
                page.Add(ep);
                if ((limit <= 0) || (servers.Count < limit))
                    continue;
                Raise(new ServerPageArgs(page));
                return "0.0.0.0:0";
            }
            Raise(new ServerPageArgs(page));
            return seed;
        }

        protected SourceMasterServerQueryResult CreateServerDictionary(IPEndPoint address)
            => new SourceMasterServerQueryResult(address, new SourceParseResult { Address = address });
    }

    public class ServerPageArgs
    {
        public ServerPageArgs(List<IPEndPoint> items) {
            Items = items;
        }

        public List<IPEndPoint> Items { get; }
    }
}