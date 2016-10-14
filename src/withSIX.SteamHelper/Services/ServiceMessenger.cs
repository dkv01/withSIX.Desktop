using System;
using System.Reactive.Disposables;
using Microsoft.AspNetCore.SignalR;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Infra;
using withSIX.Steam.Presentation.Commands;
using withSIX.Steam.Presentation.Hubs;

namespace withSIX.Steam.Presentation.Services
{
    public class ServiceMessenger : IServiceMessenger, IPresentationService, IDisposable
    {
        private readonly CompositeDisposable _dsp;
        private readonly Lazy<IHubContext<ServerHub, IServerHubClient>> _hubContext = SystemExtensions.CreateLazy(() =>
                Extensions.ConnectionManager.ServerHub);

        public ServiceMessenger(IMessageBusProxy mb) {
            _dsp = new CompositeDisposable {
                mb.Listen<Tuple<IRequestScope, ReceivedServerPageEvent>>()
                    .Subscribe(
                        x =>
                            _hubContext.Value.Clients.Client(x.Item1.ConnectionId)
                                .ServerPageReceived(x.Item2, x.Item1.RequestId)),
                mb.Listen<Tuple<IRequestScope, ReceivedServerAddressesPageEvent>>()
                    .Subscribe(
                        x =>
                            _hubContext.Value.Clients.Client(x.Item1.ConnectionId)
                                .ServerAddressesPageReceived(x.Item2, x.Item1.RequestId))
            };
        }

        public void Dispose() => _dsp.Dispose();
    }
}