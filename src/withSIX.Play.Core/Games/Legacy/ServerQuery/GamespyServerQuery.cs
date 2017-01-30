// <copyright company="SIX Networks GmbH" file="GamespyServerQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Core.Extensions;

namespace withSIX.Play.Core.Games.Legacy.ServerQuery
{
    public class GamespyServerQuery : ServerQueryBase, IServerQuery
    {
        static readonly byte[] challengePacket = {0xFE, 0xFD, 0x09};
        static readonly byte[] basePacket = {0xFE, 0xFD, 0x00};
        static readonly byte[] idPacket = {0x06, 0x08, 0x06, 0x08};
        static readonly byte[] fullInfoPacket = {0xFF, 0xFF, 0xFF, 0x01};
        static readonly string[] altGames = {"takoncopterpc"};
        static readonly byte[] magicPacket = {
            0xfe, 0xfd, 0x00, 0xe9, 0xb9, 0xa6, 0x4b, 0x1a, 0x01, 0x04, 0x08, 0x0a, 0x1f, 0x20, 0x05, 0x06,
            0x6f,
            0x10, 0x13, 0x79, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6e, 0x70, 0x72, 0x71, 0x73, 0x74, 0x75,
            0x76, 0x00, 0x00
        };
        readonly ServerAddress _address;
        readonly IServerQueryParser _parser;
        readonly string _serverBrowserTag;
        ServerQueryState _state;

        public GamespyServerQuery(ServerAddress address, string serverBrowserTag, IServerQueryParser parser) {
            if (serverBrowserTag == null) throw new ArgumentNullException(nameof(serverBrowserTag));
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            _address = address;

            _serverBrowserTag = serverBrowserTag;
            _parser = parser;
        }

        public Task UpdateAsync(ServerQueryState state) {
            state.Client = Connect(state.EndPoint);
            _state = state;

            return FetchAsync();
        }

        UdpClient Connect(IPEndPoint e) {
            var udpClient = new UdpClient(e)
            {Client = {ReceiveTimeout = DefaultReceiveTimeout, SendTimeout = DefaultSendTimeout}};
            udpClient.Connect(_address.IP, _address.Port);

            return udpClient;
        }

        async Task FetchAsync() {
            try {
                var needsChallenge = altGames.None(x => x.Equals(_serverBrowserTag));
                if (needsChallenge)
                    await ProcessChallenge().ConfigureAwait(false);
                else
                    await ProcessMagicPacket().ConfigureAwait(false);
                await ProcessFirstDataPacket().ConfigureAwait(false);
                await ProcessRemainingDataPackets().ConfigureAwait(false);
                _state.ProcessParser(_parser);
                if (_state.Success)
                    _state.Server.UpdateInfoFromResult(_state.Result);
            } catch (TimeoutException e) {
                /*
#if DEBUG
                this.Logger().FormattedDebugException(e, "Swallowed");
#endif
*/
                _state.Exception = e;
                _state.Cancel();
            } catch (ObjectDisposedException e) {
                /*
#if DEBUG
                this.Logger().FormattedDebugException(e, "Swallowed");
#endif
*/
                _state.Exception = e;
                _state.Cancel();
            } catch (Exception e) {
                _state.Exception = e;
                _state.Cancel();
                throw;
            } finally {
                _state.StopSW();
            }
        }

        async Task ProcessChallenge() {
            var firstRequestPacket = BuildChallengePacket();
            _state.StartSW();
            await
                _state.Client.SendWithTimeoutAfter(firstRequestPacket, DefaultSendTimeout).ConfigureAwait(false);
            var response = (await
                _state.Client.ReceiveWithTimeoutAfter(DefaultReceiveTimeout).ConfigureAwait(false)).Buffer;
            _state.StopSW();
            await SendChallengeResponse(response).ConfigureAwait(false);
        }

        static byte[] BuildChallengePacket() => challengePacket.Concat(idPacket).ToArray();

        async Task SendChallengeResponse(IEnumerable<byte> response) {
            var secondPacket = GetChallengeResponse(response);
            _state.StartSW();
            await
                _state.Client.SendWithTimeoutAfter(secondPacket, DefaultSendTimeout).ConfigureAwait(false);
        }

        static byte[] GetChallengeResponse(IEnumerable<byte> challengeResponse) {
            if (challengeResponse == null) throw new ArgumentNullException(nameof(challengeResponse));

            var packet = challengeResponse.Skip(5).ToArray();
            var challengeString = Encoding.ASCII.GetString(packet);
            if (challengeString == "0\0")
                return GetNoChallengeResponse();
            packet =
                BitConverter.GetBytes(challengeString.TryInt()).Reverse().ToArray();
            return basePacket.Concat(idPacket)
                .Concat(packet)
                .Concat(fullInfoPacket)
                .ToArray();
        }

        static byte[] GetNoChallengeResponse() => basePacket.Concat(idPacket)
    .Concat(fullInfoPacket)
    .ToArray();

        async Task ProcessMagicPacket() {
            _state.StartSW();
            await
                _state.Client.SendWithTimeoutAfter(magicPacket, DefaultSendTimeout).ConfigureAwait(false);
            await _state.Client.ReceiveWithTimeoutAfter(DefaultReceiveTimeout).ConfigureAwait(false);
            _state.StopSW();
            await SendMagicBasePacket().ConfigureAwait(false);
        }

        async Task SendMagicBasePacket() {
            var secondPacket = basePacket.Concat(idPacket).Concat(fullInfoPacket).ToArray();
            _state.StartSW();
            await
                _state.Client.SendWithTimeoutAfter(secondPacket, DefaultSendTimeout).ConfigureAwait(false);
        }

        async Task<byte[]> ProcessFirstDataPacket() {
            var response =
                (await
                    _state.Client.ReceiveWithTimeoutAfter(DefaultReceiveTimeout).ConfigureAwait(false)).Buffer;
            _state.StopSW();

            ProcessPacketHeader(response);
            return response;
        }

        async Task ProcessRemainingDataPackets() {
            while (_state.ReceivedPackets.Count < _state.MaxPackets) {
                var response =
                    (await
                        _state.Client.ReceiveWithTimeoutAfter(DefaultReceiveTimeout).ConfigureAwait(false)).Buffer;
                ProcessPacketHeader(response);
            }
        }

        void ProcessPacketHeader(byte[] r) {
            if (r == null) throw new ArgumentNullException(nameof(r));

            var replyHeader = r.Take(16).ToArray();
            var splitNum = replyHeader.Skip(14).Take(1).First();

            var index = (splitNum & 127);
            var last = (splitNum & 0x80) > 0;
            if (last)
                _state.MaxPackets = index + 1;
            _state.ReceivedPackets[index] = r;
        }
    }

    public static class SocketExtensions
    {
        public static async Task<int> SendWithTimeoutAfter(this UdpClient client, Byte[] list, int delay) {
            using (var token = new CancellationTokenSource())
                return await SendWithTimeoutAfter(client, list, delay, token).ConfigureAwait(false);
        }

        public static async Task<int> SendWithTimeoutAfter(this UdpClient client, Byte[] list, int delay,
            CancellationTokenSource cancel) {
            if (client == null)
                throw new NullReferenceException();

            if (list == null)
                throw new ArgumentNullException("list");

            var task = client.SendAsync(list, list.Length);

            if (task.IsCompleted || (delay == Timeout.Infinite))
                return await task.ConfigureAwait(false);

            await Task.WhenAny(task, Task.Delay(delay, cancel.Token)).ConfigureAwait(false);

            if (!task.IsCompleted) {
                client.Dispose();
                try {
                    return await task.ConfigureAwait(false);
                } catch (ObjectDisposedException) {
                    throw new TimeoutException("Send timed out.");
                }
            }

            cancel.Cancel(); // Cancel the timer! 
            return await task.ConfigureAwait(false);
        }

        public static async Task<UdpReceiveResult> ReceiveWithTimeoutAfter(this UdpClient client, int milliSecondsDelay) {
            using (var token = new CancellationTokenSource())
                return await ReceiveWithTimeoutAfter(client, milliSecondsDelay, token).ConfigureAwait(false);
        }

        public static async Task<UdpReceiveResult> ReceiveWithTimeoutAfter(this UdpClient client, int milliSecondsDelay,
            CancellationTokenSource cancel) {
            if (client == null)
                throw new NullReferenceException();

            var task = client.ReceiveAsync();

            if (task.IsCompleted || (milliSecondsDelay == Timeout.Infinite))
                return (await task.ConfigureAwait(false));

            await Task.WhenAny(task, Task.Delay(milliSecondsDelay, cancel.Token)).ConfigureAwait(false);

            if (!task.IsCompleted) {
                client.Dispose();
                try {
                    return await task.ConfigureAwait(false);
                } catch (ObjectDisposedException) {
                    throw new TimeoutException("Receive timed out.");
                }
            }

            cancel.Cancel(); // Cancel the timer! 
            return await task.ConfigureAwait(false);
        }
    }
}