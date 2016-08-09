﻿// <copyright company="SIX Networks GmbH" file="UnsubscribeFromCollectionCommandHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Play.Applications.Services.Infrastructure;
using SN.withSIX.Play.Core.Connect.Infrastructure;
using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Options;

namespace SN.withSIX.Play.Applications.UseCases.Games
{
    public class UnsubscribeFromCollectionCommandHandler :
        IAsyncRequestHandler<UnsubscribeFromCollectionCommand, Unit>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;
        readonly IUserSettingsStorage _storage;

        public UnsubscribeFromCollectionCommandHandler(IContentManager contentList, IConnectApiHandler api,
            UserSettings settings, IUserSettingsStorage storage) {
            _contentList = contentList;
            _api = api;
            _storage = storage;
        }

        public async Task<Unit> Handle(UnsubscribeFromCollectionCommand request) {
            var collection = _contentList.SubscribedCollections.First(x => x.Id == request.Id);

            try {
                await collection.Unsubscribe(_api).ConfigureAwait(false);
            } catch (NotFoundException) {}

            _contentList.SubscribedCollections.RemoveLocked(collection);
            await _storage.SaveNow().ConfigureAwait(false);
            return Unit.Value;
        }
    }
}