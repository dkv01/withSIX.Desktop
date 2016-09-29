// <copyright company="SIX Networks GmbH" file="StateMessenger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNet.SignalR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Usecases.Main;
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
        private readonly IHubContext<IStatusClientHub> _hubContext =
            GlobalHost.ConnectionManager.GetHubContext<StatusHub, IStatusClientHub>();
        private readonly IDisposable _subscription;

        public StateMessengerBus(IGameLocker gameLocker) {
            _subscription = gameLocker.LockChanged.Subscribe(Handle);
        }

        public void Dispose() {
            _subscription.Dispose();
        }

        public void Initialize() {
            // Dummy because we need to setup this listener..
        }

        private void Handle(GameLockChanged notification) {
            if (notification.IsLocked)
                _hubContext.Clients.All.LockedGame(notification.GameId, notification.CanAbort);
            else
                _hubContext.Clients.All.UnlockedGame(notification.GameId);
        }
    }

    public class StateMessenger : INotificationHandler<ContentInstalled>,
        INotificationHandler<ContentStatusChanged>, INotificationHandler<ActionTabStateUpdate>,
        INotificationHandler<UninstallActionCompleted>, INotificationHandler<GameLaunched>,
        INotificationHandler<GameTerminated>,
        IAsyncNotificationHandler<ActionNotification>
    {
        readonly IHubContext<IStatusClientHub> _hubContext =
            GlobalHost.ConnectionManager.GetHubContext<StatusHub, IStatusClientHub>();

        public Task Handle(ActionNotification notification) =>
            _hubContext.Clients.All.ActionNotification(notification);

        public void Handle(ActionTabStateUpdate notification)
            => _hubContext.Clients.All.ActionUpdateNotification(notification);

        public void Handle(ContentInstalled notification) {
            var states = notification.Content.GetStates();
            _hubContext.Clients.All.ContentStateChanged(new ContentStateChange {
                GameId = notification.GameId,
                States = states
            });
        }

        public void Handle(ContentStatusChanged notification)
            => _hubContext.Clients.All.ContentStatusChanged(notification.MapTo<ContentStatus>());

        public void Handle(GameLaunched notification) => _hubContext.Clients.All.LaunchedGame(notification.Game.Id);

        public void Handle(GameTerminated notification) => _hubContext.Clients.All.TerminatedGame(notification.Game.Id);

        public void Handle(UninstallActionCompleted notification) {
            _hubContext.Clients.All.ContentStateChanged(new ContentStateChange {
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