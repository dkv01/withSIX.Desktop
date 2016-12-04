// <copyright company="SIX Networks GmbH" file="ContentHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api.Messengers
{
    public class ContentHandler : INotificationHandler<ContentUsed>, INotificationHandler<ContentInstalled>,
        INotificationHandler<RecentItemRemoved>
    {
        readonly Lazy<IHubContext<ContentHub, IContentClientHub>> _hubContext = SystemExtensions.CreateLazy(() =>
            Extensions.ConnectionManager.ContentHub);

        public void Handle(ContentInstalled notification) {
            // TODO: Also have List<> based S-IR event instead?
            foreach (var c in notification.Content) {
                var installedContentModel = c.MapTo<InstalledContentModel>();
                _hubContext.Value.Clients.All.ContentInstalled(notification.GameId,
                    installedContentModel);
            }
        }

        public void Handle(ContentUsed notification) {
            _hubContext.Value.Clients.All.RecentItemAdded(notification.Content.GameId,
                notification.Content.MapTo<RecentContentModel>());
            _hubContext.Value.Clients.All.RecentItemUsed(notification.Content.GameId, notification.Content.Id,
                notification.Content.RecentInfo.LastUsed);
        }

        // TODO! notification.Content.GameId, 
        public void Handle(RecentItemRemoved notification) {
            _hubContext.Value.Clients.All.RecentItemRemoved(notification.Content.Id);
        }
    }
}