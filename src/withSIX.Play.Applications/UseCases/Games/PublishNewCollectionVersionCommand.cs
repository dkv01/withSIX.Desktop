// <copyright company="SIX Networks GmbH" file="PublishNewCollectionVersionCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Logging;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class PublishNewCollectionVersionCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public PublishNewCollectionVersionCommand(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class PublishNewCollectionVersionCommandHandler :
        IAsyncRequestHandler<PublishNewCollectionVersionCommand, Unit>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;
        readonly IUserSettingsStorage _storage;

        public PublishNewCollectionVersionCommandHandler(IContentManager contentList, IConnectApiHandler api,
            IUserSettingsStorage storage) {
            _contentList = contentList;
            _api = api;
            _storage = storage;
        }

        public async Task<Unit> Handle(PublishNewCollectionVersionCommand request) {
            var collection = _contentList.CustomCollections.First(x => x.Id == request.Id);

            try {
                await collection.PublishNewVersion(_api).ConfigureAwait(false);
            } catch (CollectionImageUploadException ex) {
                MainLog.Logger.FormattedWarnException(ex, "Image failure");
            }

            await _storage.SaveNow().ConfigureAwait(false);
            return Unit.Value;
        }
    }
}