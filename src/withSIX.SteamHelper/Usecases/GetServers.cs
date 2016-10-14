// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Core.Presentation;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Plugin.Arma;
using System.Linq;
using withSIX.Steam.Core.Services;
using withSIX.Steam.Infra;
using withSIX.Api.Models.Extensions;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServers : Core.Requests.GetServers, ICancellableQuery<BatchResult>, IHaveFilter
    {
    }

    public class GetServersHandler : ICancellableAsyncRequestHandler<GetServers, BatchResult>
    {
        private readonly IMessageBusProxy _mb;
        private readonly IRequestScopeLocator _scopeLocator;
        private readonly ISteamApi _steamApi;

        public GetServersHandler(ISteamApi steamApi, IMessageBusProxy mb, IRequestScopeLocator scopeLocator) {
            _steamApi = steamApi;
            _mb = mb;
            _scopeLocator = scopeLocator;
        }


        public Task<BatchResult> Handle(GetServers message, CancellationToken ct)
            => new GetServersSession(_steamApi, _mb, _scopeLocator.Scope).Handle(message, ct);

        class GetServersSession : ServerSession<GetServers>
        {
            public GetServersSession(ISteamApi steamApi, IMessageBusProxy mb, IRequestScope scope) : base(steamApi, mb, scope) {}

            protected override async Task<BatchResult> HandleInternal() {
                var obs = await (Message.IncludeDetails
                    ? Sb.GetServersInclDetails2(Ct, Builder, Message.IncludeRules)
                    : Sb.GetServers2(Ct, Builder)).ConfigureAwait(false);
                var r =
                    await
                        obs
                            .Cast<ArmaServerInfoModel>()
                            .Buffer(Message.PageSize)
                            .Do(
                                x =>
                                    SendEvent(
                                        new ReceivedServerPageEvent(
                                            x.Select(s => s.MapTo<ArmaServerInfoModel>()).ToList<ServerInfoModel>())))
                            .SelectMany(x => x)
                            .Count();
                return new BatchResult(r);
            }
        }
    }
}