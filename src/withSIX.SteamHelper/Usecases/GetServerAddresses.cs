// <copyright company="SIX Networks GmbH" file="GetServerAddresses.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Plugin.Arma;
using System.Reactive.Linq;
using System.Linq;
using withSIX.Core.Presentation;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServerAddresses : Core.Requests.GetServerAddresses, ICancellableQuery<BatchResult>,
        IHaveFilter
    {
    }

    public class GetServerAddressesHandler : ICancellableAsyncRequestHandler<GetServerAddresses, BatchResult>
    {
        private readonly IMessageBusProxy _mb;
        private readonly IRequestScopeLocator _locator;
        private readonly ISteamApi _steamApi;

        public GetServerAddressesHandler(ISteamApi steamApi, IMessageBusProxy mb, IRequestScopeLocator locator) {
            _steamApi = steamApi;
            _mb = mb;
            _locator = locator;
        }

        public Task<BatchResult> Handle(GetServerAddresses message, CancellationToken ct)
            => new GetServerAddressesSession(_steamApi, _mb, _locator.Scope).Handle(message, ct);

        class GetServerAddressesSession : ServerSession<GetServerAddresses>
        {
            public GetServerAddressesSession(ISteamApi steamApi, IMessageBusProxy mb, IRequestScope scope)
                : base(steamApi, mb, scope) {}

            protected override async Task<BatchResult> HandleInternal() {
                var obs = await Sb.GetServers2(Ct, Builder).ConfigureAwait(false);
                var r =
                    await
                        obs.Select(x => x.QueryEndPoint).Take(10)
                            // todo config limit
                            .Buffer(Message.PageSize) // todo config limit
                            .Do(x => SendEvent(new ReceivedServerIpPageEvent(x.ToList()))).Count();
                return new BatchResult(r);
            }
        }
    }
}