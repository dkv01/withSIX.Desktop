// <copyright company="SIX Networks GmbH" file="SocketExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GameServerQuery.Extensions
{
    public static class SocketExtensions
    {
        public static async Task<int> SendWithTimeoutAfter(this UdpClient client, Byte[] list, int delay, IPEndPoint ep) {
            using (var token = new CancellationTokenSource())
                return await SendWithTimeoutAfter(client, list, delay, ep, token).ConfigureAwait(false);
        }

        public static async Task<int> SendWithTimeoutAfter(this UdpClient client, Byte[] list, int delay, IPEndPoint ep,
            CancellationTokenSource cancel) {
            if (client == null)
                throw new NullReferenceException();

            if (list == null)
                throw new ArgumentNullException("list");

            var task = client.SendAsync(list, list.Length, ep);

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

        public static async Task<UdpReceiveResult> ReceiveWithTimeoutAfter(this UdpClient client, int milliSecondsDelay,
            CancellationToken cancel = default(CancellationToken)) {
            if (client == null)
                throw new NullReferenceException();

            var task = client.ReceiveAsync();

            if (task.IsCompleted || (milliSecondsDelay == Timeout.Infinite))
                return await task.ConfigureAwait(false);

            using (var cts = new CancellationTokenSource()) {
                using (cancel.Register(cts.Cancel)) {
                    await Task.WhenAny(task, Task.Delay(milliSecondsDelay, cts.Token)).ConfigureAwait(false);

                    if (!task.IsCompleted) {
                        client.Dispose();
                        try {
                            return await task.ConfigureAwait(false);
                        } catch (ObjectDisposedException) {
                            throw new TimeoutException("Receive timed out.");
                        }
                    }
                    cts.Cancel(); // Cancel the timer! 
                }
            }
            return await task.ConfigureAwait(false);
        }
    }
}