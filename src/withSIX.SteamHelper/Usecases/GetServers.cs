// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Plugin.Arma;
using withSIX.Steam.Presentation.Hubs;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServers : Core.Requests.GetServers, ICancellableQuery<BatchResult>, IRequireConnectionId, IHaveFilter, IRequireRequestId
    {
        public string ConnectionId { get; set; }
        public Guid RequestId { get; set; }
    }

    public class GetServersHandler : ICancellableAsyncRequestHandler<GetServers, BatchResult>
    {
        private readonly IMessageBusProxy _mb;
        private readonly ISteamApi _steamApi;

        public GetServersHandler(ISteamApi steamApi, IMessageBusProxy mb) {
            _steamApi = steamApi;
            _mb = mb;
        }


        public Task<BatchResult> Handle(GetServers message, CancellationToken ct)
            => new GetServerAddressesSession(_steamApi, _mb).Handle(message, ct);

        class GetServerAddressesSession : ServerSession<GetServers>
        {
            public GetServerAddressesSession(ISteamApi steamApi, IMessageBusProxy mb) : base(steamApi, mb) {}

            protected override async Task<BatchResult> HandleInternal() {
                var obs = await (Message.IncludeDetails
                    ? Sb.GetServersInclDetails2(Ct, Builder, Message.IncludeRules)
                    : Sb.GetServers2(Ct, Builder)).ConfigureAwait(false);
                var r =
                    await
                        obs
                            .Cast<ArmaServerInfoModel>()
                            .Buffer(Message.PageSize)
                            .Do(x => SendEvent(new ReceivedServerPageEvent(x)))
                            .SelectMany(x => x)
                            .Count();
                return new BatchResult(r);
            }
        }
    }
}