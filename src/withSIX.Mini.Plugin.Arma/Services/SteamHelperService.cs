// <copyright company="SIX Networks GmbH" file="SteamHelperService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery.Games.RV;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Services;
using withSIX.Mini.Applications.Services;

namespace withSIX.Mini.Plugin.Arma.Services
{
    public class ServersInfo<T>
    {
        public List<T> Servers { get; set; }
    }

    public interface ISteamHelperService {
        Task<ServersInfo<T>> GetServers<T>(uint appId, bool inclExtendedDetails, List<IPEndPoint> ipEndPoints);
    }

    public class SteamHelperService : ISteamHelperService, IDomainService
    {
        private static readonly AsyncLock _l = new AsyncLock();
        private static volatile bool _isRunning;

        public async Task<ServersInfo<T>> GetServers<T>(uint appId, bool inclExtendedDetails, List<IPEndPoint> ipEndPoints) {
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
                var tcs = new TaskCompletionSource<Unit>();
                using (var cts = new CancellationTokenSource()) {
                    var t = TaskExt.StartLongRunningTask(
                        async () => {
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
                        }, cts.Token);
                    await tcs.Task;
                    // ReSharper disable once MethodSupportsCancellation
                    t = TaskExt.StartLongRunningTask(async () => {
                        using (var drainer = new Drainer()) {
                            await drainer.Drain().ConfigureAwait(false);
                        }
                    });
                }
                _isRunning = true;
            }
        }
    }

    public class ArmaServerInfoModel
    {
        public ArmaServerInfoModel(IPEndPoint queryEndpoint) {
            QueryEndPoint = queryEndpoint;
            ConnectionEndPoint = QueryEndPoint;
            ModList = new List<ServerModInfo>();
            SignatureList = new HashSet<string>();
        }

        public AiLevel AiLevel { get; set; }

        public IPEndPoint ConnectionEndPoint { get; set; }

        public int CurrentPlayers { get; set; }

        public Difficulty Difficulty { get; set; }

        public Dlcs DownloadableContent { get; set; }

        public GameTags GameTags { get; set; }

        public HelicopterFlightModel HelicopterFlightModel { get; set; }

        public bool IsModListOverflowed { get; set; }

        public bool IsSignatureListOverflowed { get; set; }

        public bool IsThirdPersonViewEnabled { get; set; }

        public bool IsVacEnabled { get; set; }

        public bool IsWeaponCrosshairEnabled { get; set; }

        public string Map { get; set; }

        public int MaxPlayers { get; set; }

        public string Mission { get; set; }

        public List<ServerModInfo> ModList { get; set; }

        public string Name { get; set; }

        public int Ping { get; set; }

        public IPEndPoint QueryEndPoint { get; }

        public bool RequirePassword { get; set; }

        public bool RequiresExpansionTerrain { get; set; }

        public int ServerVersion { get; set; }

        public HashSet<string> SignatureList { get; set; }

        public string Tags { get; set; }

        public bool ReceivedRules { get; set; }
    }

    public class ReceivedServerEvent : IEvent
    {
        public ReceivedServerEvent(ArmaServerInfoModel serverInfo) {
            ServerInfo = serverInfo;
        }

        public ArmaServerInfoModel ServerInfo { get; }
    }
}