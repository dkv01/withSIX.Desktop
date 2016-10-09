// <copyright company="SIX Networks GmbH" file="GetServerAddresses.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using GameServerQuery;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Plugin.Arma;
using System.Reactive.Linq;
using System.Linq;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServerAddresses : Core.Requests.GetServerAddresses, ICancellableQuery<BatchResult> {}

    public class GetServerAddressesHandler : ICancellableAsyncRequestHandler<GetServerAddresses, BatchResult>
    {
        private readonly IMessageBusProxy _mb;
        private readonly ISteamApi _steamApi;

        public GetServerAddressesHandler(ISteamApi steamApi, IMessageBusProxy mb) {
            _steamApi = steamApi;
            _mb = mb;
        }

        public async Task<BatchResult> Handle(GetServerAddresses message, CancellationToken ct) {
            using (var sb = await SteamActions.CreateServerBrowser(_steamApi).ConfigureAwait(false)) {
                using (var cts = new CancellationTokenSource()) {
                    var builder = ServerFilterBuilder.FromValue(message.Filter);
                    var obs = await sb.GetServers2(cts.Token, builder);
                    var r =
                        await
                            obs
                                .Select(x => x.QueryEndPoint)
                                .Take(10) // todo config limit
                                .Buffer(message.PageSize)
                                .Do(x => _mb.SendMessage(new ReceivedServerIpPageEvent(x.ToList())))
                                .Count();
                    cts.Cancel();
                    return new BatchResult(r);
                }
            }
        }
    }
}