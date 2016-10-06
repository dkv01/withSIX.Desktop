// <copyright company="SIX Networks GmbH" file="SteamHelperService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Infra.Services;
using withSIX.Steam.Core.Services;

namespace withSIX.Mini.Infra.Data.Services
{

    public class SteamHelperService : ISteamHelperService, IInfrastructureService
    {
        private static readonly AsyncLock _l = new AsyncLock();
        private static volatile bool _isRunning;

        public async Task<ServersInfo<T>> GetServers<T>(uint appId, bool inclExtendedDetails,
            List<IPEndPoint> ipEndPoints) {
            await StartSteamHelper(appId).ConfigureAwait(false);
            var r = await new {
                IncludeDetails = true,
                IncludeRules = inclExtendedDetails,
                Addresses = ipEndPoints
            }.PostJson<ServersInfo<T>>(new Uri("http://127.0.0.66:48667/api/get-server-info")).ConfigureAwait(false);
            return r;
        }

        async Task StartSteamHelper(uint appId) {
            using (await _l.LockAsync().ConfigureAwait(false)) {
                if (_isRunning)
                    return;
                var steamH = new SteamHelperRunner();
                await LaunchSteamHelper(appId, steamH).ConfigureAwait(false);
                // ReSharper disable once MethodSupportsCancellation
                var t2 = TaskExt.StartLongRunningTask((Func<Task>) (async () => {
                    using (var drainer = new Drainer()) {
                        await drainer.Drain().ConfigureAwait(false);
                    }
                }));
                _isRunning = true;
            }
        }

        private static async Task LaunchSteamHelper(uint appId, SteamHelperRunner steamH) {
            var tcs = new TaskCompletionSource<Unit>();
            using (var cts = new CancellationTokenSource()) {
                var t = TaskExt.StartLongRunningTask(
                    (Func<Task>) (async () => {
                        try {
                            await
                                steamH.RunHelperInternal(cts.Token,
                                        steamH.GetHelperParameters("interactive", appId),
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
                    }), cts.Token);
                await tcs.Task;
            }
        }
    }
}