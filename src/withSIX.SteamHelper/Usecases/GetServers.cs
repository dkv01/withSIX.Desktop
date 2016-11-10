// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Exceptions;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Core.Services;
using withSIX.Steam.Infra;
using withSIX.Steam.Plugin.Arma;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;
using System.Reactive.Threading.Tasks;
using withSIX.Api.Models.Games;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServers : Core.Requests.GetServers, ICancellableQuery<BatchResult>, IHaveFilter {}

    public static class Cheat
    {
        public static uint AppId { get; set; }
    }

    public class GetServersHandler : ICancellableAsyncRequestHandler<GetServers, BatchResult>
    {
        private readonly IMessageBusProxy _mb;
        private readonly IRequestScopeLocator _scopeLocator;
        private readonly ISteamSessionLocator _sessionLocator;
        private readonly ISteamApi _steamApi;

        public GetServersHandler(ISteamApi steamApi, IMessageBusProxy mb, IRequestScopeLocator scopeLocator,
            ISteamSessionLocator sessionLocator) {
            _steamApi = steamApi;
            _mb = mb;
            _scopeLocator = scopeLocator;
            _sessionLocator = sessionLocator;
        }


        public Task<BatchResult> Handle(GetServers message, CancellationToken ct)
            => new GetServersSession(_steamApi, _mb, _scopeLocator.Scope, _sessionLocator).Handle(message, ct);

        class GetServersSession : ServerSession<GetServers>
        {
            private readonly ISteamSessionLocator _sessionLocator;

            public GetServersSession(ISteamApi steamApi, IMessageBusProxy mb, IRequestScope scope,
                ISteamSessionLocator sessionLocator) : base(steamApi, mb, scope) {
                _sessionLocator = sessionLocator;
            }

            protected override async Task<BatchResult> HandleInternal() {
                if (!Message.IncludeDetails)
                    throw new ValidationException(
                        "Retrieving without details is currently unsupported due to limitation in query implementation");

                if (Cheat.AppId != (uint)SteamGameIds.Arma3) {
                    using (var obs2 = new Subject<ArmaServerInfoModel>()) {
                        var s = obs2.Synchronize()
                            .Buffer(Message.PageSize)
                            .Do(x => SendEvent(new ReceivedServerPageEvent(x.ToList<ServerInfoModel>())))
                            .SelectMany(x => x)
                            .Count().ToTask();
                        var c =
                            await
                                SteamServers.GetServers(_sessionLocator, Message.IncludeRules, Message.Filter, obs2.OnNext)
                                    .ConfigureAwait(false);
                        obs2.OnCompleted();
                        return new BatchResult(await s);
                    }
                }

                var obs = await (Message.IncludeDetails
                    ? Sb.GetServersInclDetails2(Ct, Builder, Message.IncludeRules)
                    : Sb.GetServers2(Ct, Builder)).ConfigureAwait(false);
                var r =
                    await
                        obs
                            .Cast<ArmaServerInfoModel>()
                            .Buffer(Message.PageSize)
                            .Do(x => SendEvent(new ReceivedServerPageEvent(x.ToList<ServerInfoModel>())))
                            .SelectMany(x => x)
                            .Count();
                return new BatchResult(r);
            }
        }
    }
}