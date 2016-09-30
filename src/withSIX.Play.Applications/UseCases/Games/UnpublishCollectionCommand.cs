// <copyright company="SIX Networks GmbH" file="UnpublishCollectionCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class UnpublishCollectionCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public UnpublishCollectionCommand(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class UnpublishCollectionCommandHandler : IAsyncRequestHandler<UnpublishCollectionCommand, Unit>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;
        readonly UserSettings _settings;
        readonly IUserSettingsStorage _storage;

        public UnpublishCollectionCommandHandler(IContentManager contentList, IConnectApiHandler api,
            UserSettings settings, IUserSettingsStorage storage) {
            _contentList = contentList;
            _api = api;
            _settings = settings;
            _storage = storage;
        }

        public async Task<Unit> Handle(UnpublishCollectionCommand request) {
            var collection = _contentList.CustomCollections.First(x => x.Id == request.Id);

            await collection.DeleteOnline(_api, _contentList).ConfigureAwait(false);

            await _storage.SaveNow().ConfigureAwait(false);
            return Unit.Value;
        }
    }
}