// <copyright company="SIX Networks GmbH" file="ContentHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using Microsoft.AspNet.SignalR;
using ShortBus;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.Hubs;

namespace SN.withSIX.Mini.Infra.Api.Messengers
{
    public class ContentHandler : INotificationHandler<ContentFavorited>, INotificationHandler<ContentUnFavorited>,
        INotificationHandler<ContentUsed>, INotificationHandler<ContentInstalled>,
        INotificationHandler<RecentItemRemoved>
    {
        readonly IHubContext<IContentClientHub> _hubContext =
            GlobalHost.ConnectionManager.GetHubContext<ContentHub, IContentClientHub>();

        public void Handle(ContentFavorited notification) {
            _hubContext.Clients.All.ContentFavorited(notification.Content.GameId,
                notification.Content.MapTo<FavoriteContentModel>());
        }

        public void Handle(ContentInstalled notification) {
            // TODO: Also have List<> based S-IR event instead?
            foreach (var c in notification.Content) {
                var installedContentModel = c.MapTo<InstalledContentModel>();
                _hubContext.Clients.All.ContentInstalled(notification.GameId,
                    installedContentModel);
            }
        }

        public void Handle(ContentUnFavorited notification) {
            _hubContext.Clients.All.ContentUnfavorited(notification.Content.GameId, notification.Content.Id);
        }

        public void Handle(ContentUsed notification) {
            //_hubContext.Clients.All.RecentItemAdded(notification.Content.GameId,
            //  notification.Content.MapTo<RecentContentModel>());
            _hubContext.Clients.All.RecentItemUsed(notification.Content.GameId, notification.Content.Id,
                notification.Content.RecentInfo.LastUsed);
        }

        public void Handle(RecentItemRemoved notification) {
            _hubContext.Clients.All.RecentItemRemoved(notification.Content.Id);
        }
    }
}