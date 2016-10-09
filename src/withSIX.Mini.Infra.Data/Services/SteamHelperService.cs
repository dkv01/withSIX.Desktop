// <copyright company="SIX Networks GmbH" file="SteamHelperService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Infra.Services;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Mini.Infra.Data.Services
{
    public class SteamHelperService : ISteamHelperService, IInfrastructureService
    {
        private static readonly AsyncLock _l = new AsyncLock();
        private static volatile bool _isRunning;
        private static readonly Uri uri = new Uri("http://127.0.0.66:48667");
        private readonly ISteamServiceSession _session;

        public SteamHelperService(ISteamServiceSession session) {
            _session = session;
        }

        public async Task<ServersInfo<T>> GetServers<T>(uint appId, GetServers query, CancellationToken ct) {
            await StartSteamHelper(appId).ConfigureAwait(false);
            var list = new List<T>();
            await _session.GetServers<T>(query, e => list.AddRange(e), ct).ConfigureAwait(false);
            return new ServersInfo<T> { Servers = list };
        }

        public Task<List<IPEndPoint>> GetServerAddresses(uint appId, GetServerAddresses query, CancellationToken ct) {
            throw new NotImplementedException();
        }

        async Task StartSteamHelper(uint appId) {
            using (await _l.LockAsync().ConfigureAwait(false)) {
                if (_isRunning)
                    return;
                await LaunchSteamHelper(appId, new SteamHelperRunner()).ConfigureAwait(false);
                await _session.Start(appId, uri).ConfigureAwait(false);
                _isRunning = true;
            }
        }

        private static async Task LaunchSteamHelper(uint appId, SteamHelperRunner steamH) {
            var tcs = new TaskCompletionSource<Unit>();
            using (var cts = new CancellationTokenSource()) {
                var t = TaskExt.StartLongRunningTask(
                    async () => {
                        try {
                            await
                                steamH.RunHelperInternal(cts.Token,
                                        steamH.GetHelperParameters("interactive", appId)
                                            .Concat(new[] {"-b", "127.0.0.66:48667"}),
                                        (process, s) => {
                                            if (s.StartsWith("Now listening on:"))
                                                tcs.SetResult(Unit.Value);
                                        }, (proces, s) => { })
                                    .ConfigureAwait(false);
                        } catch (Exception ex) {
                            tcs.SetException(ex);
                        } finally {
                            // ReSharper disable once MethodSupportsCancellation
                            using (await _l.LockAsync().ConfigureAwait(false))
                                _isRunning = false;
                        }
                    }, cts.Token);
                await tcs.Task;
            }
        }
    }
}