// <copyright company="SIX Networks GmbH" file="StateMessenger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using ReactiveUI;
using MediatR;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Infra.Api.Hubs;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Api.Messengers
{
    public interface IStateMessengerBus {}

    public class StateMessengerBus : IStateMessengerBus, IInfrastructureService
    {
        private readonly IHubContext<IStatusClientHub> _hubContext =
            GlobalHost.ConnectionManager.GetHubContext<StatusHub, IStatusClientHub>();
        private readonly IMessageBus _messageBus;

        public StateMessengerBus(IMessageBus messageBus) {
            _messageBus = messageBus;
            messageBus.Listen<GameLockChanged>().Subscribe(Handle);
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
        INotificationHandler<GlobalLocked>, INotificationHandler<GlobalUnlocked>,
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

        public void Handle(GlobalLocked notification) => _hubContext.Clients.All.Locked();

        public void Handle(GlobalUnlocked notification) => _hubContext.Clients.All.Unlocked();

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