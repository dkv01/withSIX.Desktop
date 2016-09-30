// <copyright company="SIX Networks GmbH" file="GameLaunchHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using MediatR;
using withSIX.Play.Applications.Services;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Events;

namespace withSIX.Play.Applications.NotificationHandlers
{
    public class GameLaunchHandler : IAsyncNotificationHandler<PreGameLaunchEvent>,
        IAsyncNotificationHandler<PreGameLaunchCancelleableEvent>
    {
        readonly LaunchManager _launchManager;
        readonly ExportFactory<GamesPreLaunchEventHandler> _pregameLaunchFactory;
        readonly IUpdateManager _updateManager;

        public GameLaunchHandler(IUpdateManager updateManager, LaunchManager launchManager,
            ExportFactory<GamesPreLaunchEventHandler> pregameLaunchFactory) {
            _updateManager = updateManager;
            _launchManager = launchManager;
            _pregameLaunchFactory = pregameLaunchFactory;
        }

        // TODO: Async
        public async Task Handle(PreGameLaunchCancelleableEvent notification) {
            using (var handler = _pregameLaunchFactory.CreateExport())
                await handler.Value.Process(notification);
        }

        public async Task Handle(PreGameLaunchEvent notification) {
            _launchManager.LaunchExternalApps();
            await _updateManager.PreGameLaunch().ConfigureAwait(false);
        }
    }
}