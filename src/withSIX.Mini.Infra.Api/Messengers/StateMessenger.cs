// <copyright company="SIX Networks GmbH" file="StateMessenger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Features.Main.Servers;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api.Messengers
{
    public interface IStateMessengerBus
    {
        void Initialize();
    }

    public class StateMessengerBus : IStateMessengerBus, IInfrastructureService, IDisposable
    {
        private readonly Lazy<IHubContext<StatusHub, IStatusClientHub>> _hubContext = SystemExtensions.CreateLazy(() =>
                Extensions.ConnectionManager.StatusHub);
        private readonly Lazy<IHubContext<ServerHub, IServerHubClient>> _hubContext2 = SystemExtensions.CreateLazy(() =>
                Extensions.ConnectionManager.ServerHub);
        private readonly IDisposable _dsp;

        public StateMessengerBus(IGameLocker gameLocker, IMessageBusProxy mb) {
            CompositeDisposable dsp;
             _dsp = dsp = new CompositeDisposable();
            dsp.Add(gameLocker.LockChanged.Subscribe(Handle));
            dsp.Add(
                mb.ListenScopeEvent<ServerInfoReceived>()
                    .Do(x => _hubContext2.Value.Clients.Client(x.Item1.ConnectionId).ServerInfoReceived(x.Item2))
                    .Subscribe(x => { }, err => { }, () => { }));
            dsp.Add(mb.ListenScopeEvent<ServersPageReceived>()
                .Do(x => _hubContext2.Value.Clients.Client(x.Item1.ConnectionId).ServersPageReceived(x.Item2))
                .Subscribe(x => { }, err => { }, () => { }));
        }

        public void Dispose() {
            _dsp.Dispose();
        }

        public void Initialize() {
            // Dummy because we need to setup this listener..
        }

        private void Handle(GameLockChanged notification) {
            if (notification.IsLocked)
                _hubContext.Value.Clients.All.LockedGame(notification.GameId, notification.CanAbort);
            else
                _hubContext.Value.Clients.All.UnlockedGame(notification.GameId);
        }
    }

    public class StateMessenger : INotificationHandler<ContentInstalled>,
        INotificationHandler<ContentStatusChanged>, INotificationHandler<ActionTabStateUpdate>,
        INotificationHandler<UninstallActionCompleted>, INotificationHandler<GameLaunched>,
        INotificationHandler<GameTerminated>,
        IAsyncNotificationHandler<ActionNotification>
    {
        private readonly Lazy<IHubContext<StatusHub, IStatusClientHub>> _hubContext = SystemExtensions.CreateLazy(() =>
                Extensions.ConnectionManager.StatusHub);

        public Task Handle(ActionNotification notification) =>
            _hubContext.Value.Clients.All.ActionNotification(notification);

        public void Handle(ActionTabStateUpdate notification)
            => _hubContext.Value.Clients.All.ActionUpdateNotification(notification);

        public void Handle(ContentInstalled notification) {
            var states = notification.Content.GetStates();
            _hubContext.Value.Clients.All.ContentStateChanged(new ContentStateChange {
                GameId = notification.GameId,
                States = states
            });
        }

        public void Handle(ContentStatusChanged notification)
            => _hubContext.Value.Clients.All.ContentStatusChanged(notification.MapTo<ContentStatus>());

        public void Handle(GameLaunched notification) => _hubContext.Value.Clients.All.LaunchedGame(notification.Game.Id);

        public void Handle(GameTerminated notification) => _hubContext.Value.Clients.All.TerminatedGame(notification.Game.Id);

        public void Handle(UninstallActionCompleted notification) {
            _hubContext.Value.Clients.All.ContentStateChanged(new ContentStateChange {
                GameId = notification.Game.Id,
                States =
                    notification.UninstallLocalContentAction.Content.ToDictionary(x => x.Content.Id,
                        x => {
                            var lc = x.Content as LocalContent;
                            var state = lc != null ? lc.MapTo<ContentStatus>() : x.Content.MapTo<ContentStatus>();
                            state.State = ItemState.NotInstalled;
                            state.Version = null;
                            return state;
                        })
            });
        }
    }
}