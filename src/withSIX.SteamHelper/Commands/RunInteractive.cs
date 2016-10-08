// <copyright company="SIX Networks GmbH" file="RunInteractive.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Net;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using withSIX.Api.Models.Games;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Plugin.Arma;
using withSIX.Steam.Presentation.Hubs;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;

namespace withSIX.Steam.Presentation.Commands
{
    public class RunInteractive : BaseSteamCommand
    {
        private readonly IServiceMessenger _messenger;
        private readonly ISteamSessionFactory _steamSessionFactory;

        public RunInteractive(ISteamSessionFactory steamSessionFactory, IServiceMessenger messenger)
            : base(steamSessionFactory) {
            IsCommand("interactive", "Run in interactive mode");
            _steamSessionFactory = steamSessionFactory;
            _messenger = messenger;
        }

        public static ISteamApi SteamApi { get; private set; }

        protected override async Task<int> RunAsync(string[] remainingArguments) {
            if (AppId == (uint) SteamGameIds.Arma3) {
                await SteamActions.PerformArmaSteamAction(async api => {
                    SteamApi = api;
                    await RunWebsite(CancellationToken.None).ConfigureAwait(false);
                }, (uint) SteamGameIds.Arma3, _steamSessionFactory).ConfigureAwait(false);
            } else {
                await DoWithSteamSession(() => RunWebsite(CancellationToken.None)).ConfigureAwait(false);
            }
            return 0;
        }

        private static Task RunWebsite(CancellationToken ct)
            => new WebListener().Run(new IPEndPoint(IPAddress.Parse("127.0.0.66"), 48667), null, ct);
    }

    public interface IServiceMessenger {}

    public class ServiceMessenger : IServiceMessenger, IPresentationService, IDisposable
    {
        private readonly CompositeDisposable _dsp;
        private readonly Lazy<IHubContext<ServerHub, IServerHubClient>> _hubContext = SystemExtensions.CreateLazy(() =>
                Extensions.ConnectionManager.ServerHub);

        public ServiceMessenger(IMessageBusProxy mb) {
            _dsp = new CompositeDisposable {
                mb.Listen<ReceivedServerEvent>()
                    .Subscribe(x => _hubContext.Value.Clients.All.ServerReceived(x)),
                mb.Listen<ReceivedServerPageEvent>()
                    .Subscribe(x => _hubContext.Value.Clients.All.ServerPageReceived(x)),
                //_mb.Listen<ReceivedServerEvent>().Select(x => Observable.FromAsync(x.Raise)).Merge(1).Subscribe()
            };
        }

        public void Dispose() {
            _dsp.Dispose();
        }
    }
}