using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Plugin.Arma.Models;
using withSIX.Steam.Plugin.Arma;
using System.Linq;
using System.Reactive.Linq;
using GameServerQuery;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServerInfo : ICancellableQuery<ServerInfo>
    {
        public Guid GameId { get; set; }
        public List<IPEndPoint> Addresses { get; set; }
        public bool IncludeRules { get; set; }
        public bool IncludeDetails { get; set; }
    }

    public class GetServerInfoHandler : ICancellableAsyncRequestHandler<GetServerInfo, ServerInfo>
    {
        private readonly ISteamApi _steamApi;

        public GetServerInfoHandler(ISteamApi steamApi) {
            _steamApi = steamApi;
        }

        public async Task<ServerInfo> Handle(GetServerInfo message, CancellationToken ct) {
            using (var sb = await SteamActions.CreateServerBrowser(_steamApi).ConfigureAwait(false)) {
                using (var cts = new CancellationTokenSource()) {
                    var builder = ServerFilterBuilder.Build();
                    if ((message.Addresses != null) && message.Addresses.Any())
                        builder.FilterByAddresses(message.Addresses);

                    var obs = await (message.IncludeDetails
                        ? sb.GetServersInclDetails2(cts.Token, builder, message.IncludeRules)
                        : sb.GetServers2(cts.Token, builder));
                    var r =
                        await
                            obs.SelectMany(async x => {
                                    await new ReceivedServerEvent(x).Raise().ConfigureAwait(false);
                                    return x;
                                })
                                .Take(10) // todo config limit
                                .ToList();
                    cts.Cancel();
                    return new ServerInfo {Servers = r};
                }
            }
        }
    }


    public class ServerInfo
    {
        public ICollection<ArmaServerInfo> Servers { get; set; }
    }
}
