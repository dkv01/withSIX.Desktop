// <copyright company="SIX Networks GmbH" file="ApiEventHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Api.Models.Content.v2;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class ApiHashesEvent : IAsyncDomainEvent
    {
        public ApiHashesEvent(ApiHashes hashes) {
            Hashes = hashes;
        }

        public ApiHashes Hashes { get; }
    }

    public class ApiEventHandler : IAsyncNotificationHandler<ApiHashesEvent>
    {
        readonly Random _random;
        readonly IContentManager _syncManager;

        public ApiEventHandler(IContentManager syncManager) {
            _syncManager = syncManager;
            _random = new Random();
        }

        public async Task Handle(ApiHashesEvent notification) {
            await Task.Delay(TimeSpan.FromSeconds(_random.Next(3, 15))).ConfigureAwait(false);
            await _syncManager.Sync(notification.Hashes, true).ConfigureAwait(false);
        }
    }
}