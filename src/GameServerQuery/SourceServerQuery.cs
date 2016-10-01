// <copyright company="SIX Networks GmbH" file="SourceServerQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GameServerQuery.Extensions;

namespace GameServerQuery
{
    public class SourceServerQuery : ServerQueryBase, IServerQuery
    {
        private static readonly byte[] emptyChallenge = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
        private static readonly byte[] a2SPlayerRequestH = {0xFF, 0xFF, 0xFF, 0xFF, 0x55};
        private static readonly byte[] a2SRulesRequestH = {0xFF, 0xFF, 0xFF, 0xFF, 0x56};

        private static readonly byte[] a2SInfoRequest = {
            0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20,
            0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
        };

        //static readonly string[] altGames = {}; //FUTURE: some games may need special processing
        private readonly IPEndPoint _remoteEp;
        private readonly ServerQueryState _state;

        public SourceServerQuery(ServerQueryState state, string serverBrowserTag) {
            Contract.Requires<ArgumentNullException>(state != null);
            Contract.Requires<ArgumentNullException>(serverBrowserTag != null);
            _state = state;
            _state.MaxPackets = state.HandlePlayers ? 3 : 2;
            _remoteEp = new IPEndPoint(_state.Server.Address.Address, _state.Server.Address.Port);
        }

        public async Task UpdateAsync() {
            await TryProcessPackets().ConfigureAwait(false);
            //_state.UpdateServer(); // We call this from the Queue, and we should call this manually otherwise!
        }

        private UdpClient Connect(IPEndPoint e) {
            var udpClient = new UdpClient(e) {
                Client = {ReceiveTimeout = DefaultReceiveTimeout, SendTimeout = DefaultSendTimeout}
            };
            //udpClient.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.Linger, new LingerOption(true, 0));
            return udpClient;
        }

        private async Task TryProcessPackets() {
            try {
                _state.UpdateStatus(Status.InProgress);
                _state.Client = Connect(_state.EndPoint);
                await ProcessAllPackets().ConfigureAwait(false);
                _state.UpdateStatus(Status.SuccessReceive);
            } catch (TimeoutException e) {
                /*
#if DEBUG
                this.Logger().FormattedDebugException(e, "Swallowed");
#endif
*/
                _state.Exception = e;
                _state.UpdateStatus(Status.Cancelled);
            } catch (ObjectDisposedException e) {
                /*
#if DEBUG
                this.Logger().FormattedDebugException(e, "Swallowed");
#endif
*/
                _state.Exception = e;
                _state.UpdateStatus(Status.Cancelled);
            } catch (Exception e) {
                _state.Exception = e;
                _state.UpdateStatus(Status.Failure);
                throw;
            }
        }

        private async Task ProcessAllPackets() {
            try {
                _state.ReceivedPackets[0] = await GetInfo().ConfigureAwait(false);
                _state.ReceivedPackets[1] = await GetRules().ConfigureAwait(false);
                if (_state.HandlePlayers)
                    _state.ReceivedPackets[2] = await GetPlayers().ConfigureAwait(false);
            } finally {
                _state.TryClose();
            }
        }

        private async Task<byte[]> GetInfo() {
            var response = await SendAndReceiveUntilEnd(a2SInfoRequest).ConfigureAwait(false);
            if (response[4] != 0x49)
                throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x49.", response[4]));
            return response;
        }

        private async Task<byte[]> GetPlayers() {
            var response = await GetChallengeResponse(0x55).ConfigureAwait(false);
            switch (response[4]) {
            case 0x41: //challenge
                return
                    await
                        SendAndReceiveUntilEnd(GetPlayerRequestPacket(response.Skip(5).ToArray()))
                            .ConfigureAwait(false);
            case 0x44: //no challenge needed info
                return response;
            default:
                throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x44.", response[4]));
            }
        }

        private static byte[] GetPlayerRequestPacket(IEnumerable<byte> challengeBytes) {
            return a2SPlayerRequestH.Concat(challengeBytes).ToArray();
        }

        private async Task<byte[]> GetRules() {
            var response = await GetChallengeResponse(0x56).ConfigureAwait(false);
            switch (response[4]) {
            case 0x41: //challenge
                return
                    await
                        SendAndReceiveUntilEnd(GetRulesRequestPacket(response.Skip(5).ToArray()))
                            .ConfigureAwait(false);
            case 0x45: //no challenge needed info
                return response;
            default:
                throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
            }
        }

        private static byte[] GetRulesRequestPacket(IEnumerable<byte> challengeBytes) {
            return a2SRulesRequestH.Concat(challengeBytes).ToArray();
        }

        private Task<byte[]> GetChallengeResponse(byte b) {
            emptyChallenge[4] = b;
            return SendAndReceiveSingle(emptyChallenge);
        }

        private async Task<byte[]> SendAndReceiveSingle(byte[] msg) {
            _state.StartSW();
            try {
                await SendPacket(msg).ConfigureAwait(false);
                var response = await Receive().ConfigureAwait(false);
                return response.Buffer;
            } finally {
                _state.StopSW();
            }
        }

        private async Task<byte[]> SendAndReceiveUntilEnd(byte[] msg) {
            _state.StartSW();
            var i = 0;
            try {
                await SendPacket(msg).ConfigureAwait(false);
                byte[] response = null;
                var multiStatus = new MultiStatus();
                while (response == null) {
                    try {
                        response = await ReceiveMultiple(multiStatus).ConfigureAwait(false);
                    } finally {
                        if (i == 0)
                            _state.StopSW();
                    }
                    i++;
                }
                return response;
            } finally {
                if (i == 0)
                    _state.StopSW();
            }
        }

        private async Task<byte[]> ReceiveMultiple(MultiStatus status) {
            // TODO: Keep looping for multi packets (process each header to find if there are more)
            // and concatenate them in the right order
            var response = await Receive().ConfigureAwait(false);
            return ProcessPacketHeader(response.Buffer, status);
        }

        private Task SendPacket(byte[] msg) => _state.Client.SendWithTimeoutAfter(msg, DefaultSendTimeout, _remoteEp);

        private Task<UdpReceiveResult> Receive() => _state.Client.ReceiveWithTimeoutAfter(DefaultReceiveTimeout);

        private byte[] ProcessPacketHeader(byte[] reply, MultiStatus status) {
            Contract.Requires<ArgumentNullException>(reply != null);

            var pos = 0;
            byte[] header = {0xFF, 0xFF, 0xFF};
            var match = header.All(b => reply[pos++] == b);
            if (!match)
                return null;

            if (reply[pos] == 0xFF)
                return reply;
            if (reply[pos] == 0xFE)
                return CollectPacket(reply, ++pos, status);
            return null;
        }

        private byte[] CollectPacket(byte[] reply, int pos, MultiStatus status) {
            //TODO: test if this works on old source server or mockup
            var id = BitConverter.ToInt32(reply, pos);
            pos += 4;
            var bzipped = (id & 0x80000000) != 0;
            var total = reply[pos++];
            var number = reply[pos++];
            pos += 2; //ignoring next short
            if (bzipped) {
                throw new Exception("bzip2 compression not implemented");
                /*
                if (!status.BzipDict.ContainsKey(id))
                {
                    status.BzipDict[id] = new byte[total + 2][];
                    status.BzipDict[id][total] = new byte[1];
                }
                if (number == 0)
                {
                    status.BzipDict[id][total + 1] = reply.Skip(pos).Take(4).ToArray();
                    pos += 4;
                }
                status.BzipDict[id][number] = reply.Skip(pos).ToArray();
                var runTotal = status.BzipDict[id][total][0]++;
                if (runTotal == total) JoinPackets(id, total, true);
                 */
            }
            //else
            if (!status.PlainDict.ContainsKey(id)) {
                status.PlainDict[id] = new byte[total + 1][];
                status.PlainDict[id][total] = new byte[1];
            }
            status.PlainDict[id][number] = reply.Skip(pos).ToArray();
            var runTotal = status.PlainDict[id][total][0]++;
            if (runTotal == total)
                return JoinPackets(id, total, false, status);

            return null;
        }

        private byte[] JoinPackets(int id, byte total, bool bzipped, MultiStatus status) {
            var multiPkt = bzipped ? status.BzipDict[id] : status.PlainDict[id];
            var len = multiPkt.Sum(pkt => pkt.Length);
            for (var i = total; i < multiPkt.Length; i++)
                len -= multiPkt[i].Length;
            var replyPkt = new byte[len];
            var pos = 0;
            for (var i = 0; i < total; i++) {
                Buffer.BlockCopy(multiPkt[i], 0, replyPkt, pos, multiPkt[i].Length);
                pos += multiPkt[i].Length;
            }
            if (bzipped) {
                //var input = new MemoryStream(replyPkt);
                //var output = new MemoryStream();
                //BZip2.Decompress(input, output, true);
                //replyPkt = output.ToArray();
            }
            var valid = true;
            if (total + 1 < multiPkt.Length) {
                //var size = BitConverter.ToInt32(multiPkt[total + 1], 0);
                //var crc = BitConverter.ToInt32(multiPkt[total + 1], 4);
                //if (replyPkt.Length * 8 != size) valid = false;
                //var checkCrc = new Crc32();
                //checkCrc.Update(replyPkt);
                //if (checkCrc.Value != crc) valid = false;
            }

            if (!valid) {
                throw new Exception("split packet not decompressed properly");
                //TODO: check if at least header is intact so query can be retried
            }
            return replyPkt;
        }

        private class MultiStatus
        {
            public readonly Dictionary<int, byte[][]> BzipDict = new Dictionary<int, byte[][]>(10);
            public readonly Dictionary<int, byte[][]> PlainDict = new Dictionary<int, byte[][]>(10);
        }
    }
}