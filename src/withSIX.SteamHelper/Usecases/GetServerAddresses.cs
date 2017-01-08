// <copyright company="SIX Networks GmbH" file="GetServerAddresses.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Infra;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;

namespace withSIX.Steam.Presentation.Usecases
{
    [Obsolete("What would you do with just the addresses?")]
    public class GetServerAddresses : Core.Requests.GetServerAddresses, IAsyncQuery<BatchResult>,
        IHaveFilter {}

    public class GetServerAddressesHandler : ICancellableAsyncRequestHandler<GetServerAddresses, BatchResult>
    {
        private readonly IRequestScopeLocator _locator;
        private readonly IMessageBusProxy _mb;
        private readonly ISteamSessionLocator _sessionLocator;

        public GetServerAddressesHandler(IMessageBusProxy mb, IRequestScopeLocator locator,
            ISteamSessionLocator sessionLocator) {
            _mb = mb;
            _locator = locator;
            _sessionLocator = sessionLocator;
        }

        public Task<BatchResult> Handle(GetServerAddresses message, CancellationToken ct)
            => new GetServerAddressesSession(_mb, _locator.Scope, _sessionLocator).Handle(message, ct);

        class GetServerAddressesSession : ServerSession<GetServerAddresses>
        {
            public GetServerAddressesSession(IMessageBusProxy mb, IRequestScope scope,
                ISteamSessionLocator sessionLocator)
                : base(mb, scope, sessionLocator) {
            }

            protected override async Task<BatchResult> HandleInternal() {
                using (var scheduler = new EventLoopScheduler()) {
                    using (var obs2 = new Subject<IPEndPoint>()) {
                        var s = obs2.Synchronize()
                            .ObserveOn(scheduler)
                            .Buffer(Message.PageSize)
                            .Do(x => SendEvent(new ReceivedServerAddressesPageEvent(x.ToList())))
                            .SelectMany(x => x)
                            .Count()
                            .ToTask();
                        var c =
                            await
                                SteamServers.GetServers(SessionLocator, false, Message.Filter,
                                        x => obs2.OnNext(x.QueryEndPoint))
                                    .ConfigureAwait(false);
                        obs2.OnCompleted();
                        return new BatchResult(await s);
                    }
                }
            }
        }
    }
}