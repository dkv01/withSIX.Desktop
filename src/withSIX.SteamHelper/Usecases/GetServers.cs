// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Concurrency;
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
using withSIX.Steam.Api.Helpers;

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
            private readonly ISteamApi _steamApi;
            public GetServersSession(ISteamApi steamApi, IMessageBusProxy mb, IRequestScope scope,
                ISteamSessionLocator sessionLocator) : base(mb, scope, sessionLocator) {
                _steamApi = steamApi;
            }

            protected override Task<BatchResult> HandleInternal() {
                if (!Message.IncludeDetails)
                    throw new ValidationException(
                        "Retrieving without details is currently unsupported due to limitation in query implementation");

                return Cheat.AppId == (uint) SteamGameIds.Arma3 ? GetArma3SteamServers() : GetSteamServers();
            }

            private async Task<BatchResult> GetArma3SteamServers() {
                using (var scheduler = new EventLoopScheduler()) {
                    using (var sb = await CreateArma3ServerBrowser().ConfigureAwait(false)) {
                        var obs = await (Message.IncludeDetails
                            ? sb.GetServersInclDetails2(Ct, Builder, Message.IncludeRules)
                            : sb.GetServers2(Ct, Builder)).ConfigureAwait(false);
                        var r =
                            await
                                obs.Synchronize()
                                    .ObserveOn(scheduler)
                                    .Cast<ArmaServerInfoModel>()
                                    .Buffer(Message.PageSize)
                                    .Do(x => SendEvent(new ReceivedServerPageEvent(x.ToList<ServerInfoModel>())))
                                    .SelectMany(x => x)
                                    .Count();
                        return new BatchResult(r);
                    }
                }
            }

            private async Task<BatchResult> GetSteamServers() {
                using (var scheduler = new EventLoopScheduler()) {
                    using (var obs2 = new Subject<ArmaServerInfoModel>()) {
                        var s = obs2.Synchronize()
                            .ObserveOn(scheduler)
                            .Buffer(Message.PageSize)
                            .Do(x => SendEvent(new ReceivedServerPageEvent(x.ToList<ServerInfoModel>())))
                            .SelectMany(x => x)
                            .Count()
                            .ToTask();
                        var c =
                            await
                                SteamServers.GetServers(SessionLocator, Message.IncludeRules, Message.Filter,
                                        obs2.OnNext)
                                    .ConfigureAwait(false);
                        obs2.OnCompleted();
                        return new BatchResult(await s);
                    }
                }
            }

            protected Task<ServerBrowser> CreateArma3ServerBrowser() => SteamActions.CreateServerBrowser(_steamApi);
        }
    }
}