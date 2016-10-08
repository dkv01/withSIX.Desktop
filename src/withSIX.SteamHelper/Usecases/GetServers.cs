using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Plugin.Arma;

namespace withSIX.Steam.Presentation.Usecases
{
    public class GetServers : Core.Requests.GetServers, ICancellableQuery<BatchResult>
    {
    }

    public class GetServersHandler : ICancellableAsyncRequestHandler<GetServers, BatchResult>
    {
        private readonly IMessageBusProxy _mb;
        private readonly ISteamApi _steamApi;

        public GetServersHandler(ISteamApi steamApi, IMessageBusProxy mb) {
            _steamApi = steamApi;
            _mb = mb;
        }

        public async Task<BatchResult> Handle(GetServers message, CancellationToken ct) {
            using (var sb = await SteamActions.CreateServerBrowser(_steamApi).ConfigureAwait(false)) {
                using (var cts = new CancellationTokenSource()) {
                    var builder = ServerFilterBuilder.FromValue(message.Filter);
                    var obs = await (message.IncludeDetails
                        ? sb.GetServersInclDetails2(cts.Token, builder, message.IncludeRules)
                        : sb.GetServers2(cts.Token, builder));
                    var r =
                        await
                            obs
                                //.Do(x => _mb.SendMessage(new ReceivedServerEvent(x)))
                                .Cast<ArmaServerInfoModel>()
                                .Buffer(message.PageSize)
                                .Do(x => _mb.SendMessage(new ReceivedServerPageEvent(x)))
                                .SelectMany(x => x)
                                .Count();
                    cts.Cancel();
                    return new BatchResult(r);
                }
            }
        }
    }
}