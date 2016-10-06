// <copyright company="SIX Networks GmbH" file="ArmaServerQurery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GameServerQuery;
using GameServerQuery.Parsers;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Servers;
using withSIX.Core.Logging;
using withSIX.Core.Services;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.GameLauncher;
using withSIX.Mini.Plugin.Arma.Models;

namespace withSIX.Mini.Plugin.Arma.Services
{
    public interface IArmaServerQuery : IServerQuery
    {
        Task<List<Server>> GetServers(uint appId, IReadOnlyCollection<IPEndPoint> addresses, bool inclExtendedDetails);
    }

    public class ArmaServerQuery : IArmaServerQuery, IDomainService
    {
        private readonly ISteamHelperService _steamHelperService;

        public ArmaServerQuery(ISteamHelperService steamHelperService) {
            _steamHelperService = steamHelperService;
        }

        public Task<List<Server>> GetServers(uint appId, IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails) => inclExtendedDetails
            ? GetServersFromSteam(appId, addresses, inclExtendedDetails)
            : GetFromGameServerQuery(addresses, inclExtendedDetails);

        async Task<List<Server>> GetServersFromSteam(uint appId, IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails) {
            // Ports adjusted because it expects the Connection Port!
            var ipEndPoints = addresses.Select(x => new IPEndPoint(x.Address, x.Port - 1)).ToList();
            var r =
                await
                    _steamHelperService.GetServers<ArmaServerInfoModel>(appId, inclExtendedDetails, ipEndPoints)
                        .ConfigureAwait(false);
            return r.Servers.Select(x => x.MapTo<ServerInfo<ArmaServerInfoModel>>()).ToList<Server>();
        }

        private static async Task<List<Server>> GetFromGameServerQuery(
            IReadOnlyCollection<IPEndPoint> addresses, bool inclPlayers) {
            var infos = new List<Server>();
            var q = new ReactiveSource();
            using (var client = q.CreateUdpClient())
                foreach (var a in addresses) {
                    var serverInfo = new ArmaServerWithPing {QueryAddress = a};
                    infos.Add(serverInfo);
                    try {
                        var results = await q.ProcessResults(q.GetResults(new[] {serverInfo.QueryAddress}, client));
                        var r = (SourceParseResult) results.Settings;
                        r.MapTo(serverInfo);
                        serverInfo.Ping = results.Ping;
                        /*
                        var tags = r.Keywords;
                        if (tags != null) {
                            var p = GameTags.Parse(tags);
                            p.MapTo(server);
                        }
                        */
                    } catch (Exception ex) {
                        MainLog.Logger.FormattedWarnException(ex, "While processing server " + serverInfo.QueryAddress);
                    }
                }
            return infos;
        }
    }

    class ArmaServerWithPing : ArmaServer
    {
        public int Ping { get; set; }
    }
}