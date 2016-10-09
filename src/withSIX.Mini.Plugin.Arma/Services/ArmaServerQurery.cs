// <copyright company="SIX Networks GmbH" file="ArmaServerQurery.cs">
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
using GameServerQuery.Games.RV;
using GameServerQuery.Parsers;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Servers;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;
using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.GameLauncher;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Mini.Plugin.Arma.Services
{
    public interface IArmaServerQuery : IServerQuery
    {
        Task<List<Server>> GetServerInfo(uint appId, IReadOnlyCollection<IPEndPoint> addresses, bool inclExtendedDetails);
        Task<List<IPEndPoint>> GetServerAddresses(uint appId, Func<List<IPEndPoint>, Task> act, CancellationToken cancelToken);
    }

    public class ArmaServerQuery : IArmaServerQuery, IDomainService
    {
        private readonly ISteamHelperService _steamHelperService;

        public ArmaServerQuery(ISteamHelperService steamHelperService) {
            _steamHelperService = steamHelperService;
        }

        public Task<List<Server>> GetServerInfo(uint appId, IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails) => inclExtendedDetails
            ? GetServersFromSteam(appId, addresses, inclExtendedDetails)
            : GetFromGameServerQuery(addresses, inclExtendedDetails);

        public async Task<List<IPEndPoint>> GetServerAddresses(uint appId, Func<List<IPEndPoint>, Task> act, CancellationToken cancelToken) {
            var f = ServerFilterBuilder.Build()
                .FilterByGame("arma3");
            var master = new SourceMasterQuery(f.Value);
            var r = await master.GetParsedServersObservable(cancelToken)
                .Select(x =>
                    Observable.FromAsync(async () => {
                        await act(x.Items);
                        return x;
                    }))
                .Merge(1)
                .SelectMany(x => x.Items)
                .ToList();
            return r.ToList();
        }

        async Task<List<Server>> GetServersFromSteam(uint appId, IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails) {
            // Ports adjusted because it expects the Connection Port!
            var ipEndPoints = addresses.Select(x => new IPEndPoint(x.Address, x.Port - 1)).ToList();
            var filter = ServerFilterBuilder.Build().FilterByAddresses(ipEndPoints);
            var r =
                await
                    _steamHelperService.GetServers<ArmaServerInfoModel>(appId,
                            new GetServers {
                                Filter = filter.Value,
                                IncludeDetails = inclExtendedDetails,
                                IncludeRules = true,
                                PageSize = 1
                            }, CancellationToken.None)
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


    public class ArmaServerInfoModel
    {
        public ArmaServerInfoModel(IPEndPoint queryEndpoint) : this() {
            QueryEndPoint = queryEndpoint;
            ConnectionEndPoint = QueryEndPoint;
        }

        public ArmaServerInfoModel() {
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

        public IPEndPoint QueryEndPoint { get; protected set; }

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

    public abstract class ReceivedServerPageEvent<T> : IEvent
    {
        protected ReceivedServerPageEvent(IList<T> servers) {
            Servers = servers;
        }

        public IList<T> Servers { get; }
    }

    public class ReceivedServerPageEvent : ReceivedServerPageEvent<ArmaServerInfoModel>
    {
        public ReceivedServerPageEvent(IList<ArmaServerInfoModel> servers) : base(servers) {}
    }

    public class ReceivedServerIpPageEvent : ReceivedServerPageEvent<IPEndPoint>
    {
        public ReceivedServerIpPageEvent(IList<IPEndPoint> servers) : base(servers) { }
    }
}