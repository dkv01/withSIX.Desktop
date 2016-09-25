// <copyright company="SIX Networks GmbH" file="SourceMasterQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GameServerQuery.Extensions;

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
        };

        readonly IDictionary<string, string> _filterSb;
        readonly Region _region;
        protected readonly string ServerBrowserTag;

        public SourceMasterQuery(string serverBrowserTag, Region region = Region.All) {
            ServerBrowserTag = serverBrowserTag;
            _region = region;
            _filterSb = new Dictionary<string, string>();
            SetFilter("type", "d");
            if (!serverBrowserTag.Equals(""))
                SetFilter("gamedir", serverBrowserTag);
        }

        string Filter
        {
            get { return String.Join("", _filterSb.Select(x => String.Format(@"\{0}\{1}", x.Key, x.Value))); }
        }

        public virtual async Task<IEnumerable<ServerQueryResult>> GetParsedServers(bool forceLocal = false,
            int limit = 0) {
            var serversResult = await RetrieveAsync(limit).ConfigureAwait(false);
            return serversResult.Select(CreateServerDictionary);
        }

        void SetFilter(string name, string value) {
            _filterSb[name] = value;
        }

        protected async Task<List<IPEndPoint>> RetrieveAsync(int limit, IPEndPoint remote = null, int tried = -1) {
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
                lastSeed = seed;
                var msg = BuildMessage(seed);

                if (await udpClient.SendAsync(msg, msg.Length, remote).ConfigureAwait(false) != msg.Length)
                    throw new Exception("Send failed.");
                byte[] response = null;
                var timedOut = false;
                try {
                    response =
                        (await udpClient.ReceiveWithTimeoutAfter(DefaultReceiveTimeout).ConfigureAwait(false)).Buffer;
                } catch (TimeoutException) {
                    timedOut = true;
                    if (cur == -1 || tried != -1 || i != 0) {
                        throw new TimeoutException(String.Format("Received timeout on try: {0} packet: {1}",
                            tried == -1 ? 1 : 2, i));
                    }
                }
                //only retries when remote was not passed in, on the first try, and when the first packet was never received
                if (timedOut)
                    return await RetrieveAsync(limit, null, cur).ConfigureAwait(false);
                seed = ParseResponse(servers, response, limit);
                if (seed == null)
                    throw new Exception("Bad packet recieved.");
                i++;
            } while (i < maxIterations && !seed.Equals("0.0.0.0:0") && !seed.Equals(lastSeed));
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

        static string ParseResponse(ICollection<IPEndPoint> servers, byte[] reply, int limit) {
            var seed = string.Empty;
            var pos = 0;
            byte[] header = {0xFF, 0xFF, 0xFF, 0xFF, 0x66, 0x0A};
            if (!header.All(t => reply[pos++] == t))
                return null;
            while (pos < reply.Length) {
                var ip = String.Format("{0}.{1}.{2}.{3}", reply[pos++], reply[pos++], reply[pos++], reply[pos++]);
                byte[] b = {reply[pos + 1], reply[pos]};
                var port = BitConverter.ToUInt16(b, 0);
                pos += 2;

                seed = ip + ":" + port;
                if (seed.Equals("0.0.0.0:0"))
                    break;
                servers.Add(seed.ToIPEndPoint());
                if (limit > 0 && servers.Count >= limit)
                    return "0.0.0.0:0";
            }
            return seed;
        }

        protected SourceMasterServerQueryResult CreateServerDictionary(IPEndPoint address) {
            return new SourceMasterServerQueryResult(new Dictionary<string, string> {
                {"address", address.ToString()},
                {"folder", ServerBrowserTag}
            }) {Address = address};
        }
    }
}