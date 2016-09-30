// <copyright company="SIX Networks GmbH" file="DeleteCollectionCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Extensions;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class DeleteCollectionCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public DeleteCollectionCommand(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class DeleteCollectionCommandHandler : IAsyncRequestHandler<DeleteCollectionCommand, Unit>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;
        readonly IUserSettingsStorage _storage;

        public DeleteCollectionCommandHandler(IContentManager contentList, IConnectApiHandler api,
            IUserSettingsStorage storage) {
            _contentList = contentList;
            _api = api;
            _storage = storage;
        }

        public async Task<Unit> Handle(DeleteCollectionCommand request) {
            var collection = _contentList.CustomCollections.First(x => x.Id == request.Id);

            if (collection.PublishedId.HasValue) {
                try {
                    await collection.DeleteOnline(_api, _contentList).ConfigureAwait(false);
                } catch (NotFoundException) {}
            }

            _contentList.CustomCollections.RemoveLocked(collection);

            await _storage.SaveNow().ConfigureAwait(false);
            return Unit.Value;
        }
    }
}