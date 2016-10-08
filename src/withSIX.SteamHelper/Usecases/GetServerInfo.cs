// <copyright company="SIX Networks GmbH" file="GetServerInfo.cs">
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
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Plugin.Arma;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServerInfo : Core.Requests.GetServerInfo, ICancellableQuery<ServerInfo>
    {
    }

    public class GetServerInfoHandler : ICancellableAsyncRequestHandler<GetServerInfo, ServerInfo>
    {
        private readonly IMessageBusProxy _mb;
        private readonly ISteamApi _steamApi;

        public GetServerInfoHandler(ISteamApi steamApi, IMessageBusProxy mb) {
            _steamApi = steamApi;
            _mb = mb;
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
                            obs.Do(x => _mb.SendMessage(new ReceivedServerEvent(x)))
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