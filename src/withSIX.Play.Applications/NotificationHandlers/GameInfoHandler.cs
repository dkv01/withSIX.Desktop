// <copyright company="SIX Networks GmbH" file="GameInfoHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Events;

namespace withSIX.Play.Applications.NotificationHandlers
{
    public class GameInfoHandler : IAsyncNotificationHandler<ActiveGameChanged>,
        IAsyncNotificationHandler<SubGamesChanged>
    {
        readonly IContentManager _contentManager;

        public GameInfoHandler(IContentManager contentManager) {
            _contentManager = contentManager;
        }

        public Task Handle(ActiveGameChanged notification) => _contentManager.Handle(notification);

        public Task Handle(SubGamesChanged notification) => _contentManager.Handle(notification);
    }
}