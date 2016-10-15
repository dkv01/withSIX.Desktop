﻿// <copyright company="SIX Networks GmbH" file="ArmaServerQurery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery;
using GameServerQuery.Parsers;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Servers;
using withSIX.Api.Models.Servers.RV;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Core.Services;
using withSIX.Mini.Core.Games.Services.GameLauncher;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Mini.Plugin.Arma.Services
{
    public interface IArmaServerQuery : IServerQuery
    {
        Task<BatchResult> GetServerInfo(uint appId, IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails, Action<Server> act);

        Task<BatchResult> GetServerAddresses(uint appId, Action<List<IPEndPoint>> act,
            CancellationToken cancelToken);
    }

    public class ArmaServerQuery : IArmaServerQuery, IDomainService
    {
        private readonly ISteamHelperService _steamHelperService;

        public ArmaServerQuery(ISteamHelperService steamHelperService) {
            _steamHelperService = steamHelperService;
        }

        public Task<BatchResult> GetServerInfo(uint appId, IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails, Action<Server> act) => inclExtendedDetails
            ? TryGetServersFromSteam(appId, addresses, inclExtendedDetails, act)
            : GetFromGameServerQuery(addresses, inclExtendedDetails, act);

        private async Task<BatchResult> TryGetServersFromSteam(uint appId, IReadOnlyCollection<IPEndPoint> addresses, bool inclExtendedDetails, Action<Server> act) {
            try {
                return await GetServersFromSteam(appId, addresses, inclExtendedDetails, act).ConfigureAwait(false);
            } catch(Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Problem while processing info from Steam!");
                return await GetFromGameServerQuery(addresses, inclExtendedDetails, act).ConfigureAwait(false);
            }
        }
        public async Task<BatchResult> GetServerAddresses(uint appId, Action<List<IPEndPoint>> act,
            CancellationToken cancelToken) {
            var f = ServerFilterBuilder.Build()
                .FilterByGame("arma3");
            var master = new SourceMasterQuery(f.Value);
            return new BatchResult(await master.GetParsedServersObservable(cancelToken)
                .Do(x => act(x.Items))
                .SelectMany(x => x.Items)
                .Count());
        }

        async Task<BatchResult> GetServersFromSteam(uint appId, IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails, Action<Server> act) {
            var includeRules = true;
            var cvt = GetConverter(includeRules);
            var r = await Task.WhenAll(addresses.Batch(15).Select(b => {
                // Ports adjusted because it expects the Connection Port!
                var filter =
                    ServerFilterBuilder.Build()
                        .FilterByAddresses(b.Select(x => new IPEndPoint(x.Address, x.Port - 1)).ToList());
                return _steamHelperService.GetServers<ArmaServerInfoModel>(appId,
                    new GetServers {
                        Filter = filter.Value,
                        IncludeDetails = inclExtendedDetails,
                        IncludeRules = includeRules,
                        PageSize = 1
                    }, CancellationToken.None, x => {
                        foreach (var s in x.Select(cvt))
                            act(s);
                    });
            }));
            return new BatchResult(r.Sum(x => x.Count));
        }

        private static Func<ArmaServerInfoModel, Server> GetConverter(bool includeRules) {
            Func<ArmaServerInfoModel, Server> cvt;
            if (includeRules)
                cvt = x => x.MapTo<ArmaServerInclRules>();
            else
                cvt = x => x.MapTo<ArmaServer>();
            return cvt;
        }

        private static async Task<BatchResult> GetFromGameServerQuery(
            IReadOnlyCollection<IPEndPoint> addresses, bool inclPlayers, Action<Server> act) {
            var q = new ReactiveSource();
            using (var client = q.CreateUdpClient()) {
                return new BatchResult(await q.ProcessResults(q.GetResults(addresses, client))
                    .Do(x => {
                        var r = (SourceParseResult) x.Settings;
                        var serverInfo = r.MapTo<ArmaServer>();
                        serverInfo.Ping = x.Ping;
                        act(serverInfo);
                    }).Count());
            }
        }
    }
}