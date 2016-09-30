// <copyright company="SIX Networks GmbH" file="ChangeCollectionScopeCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Collections;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class ChangeCollectionScopeCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public ChangeCollectionScopeCommand(Guid id, CollectionScope scope) {
            Id = id;
            Scope = scope;
        }

        public Guid Id { get; }
        public CollectionScope Scope { get; }
    }

    public class ChangeCollectionScopeCommandHandler : IAsyncRequestHandler<ChangeCollectionScopeCommand, Unit>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;
        readonly IUserSettingsStorage _storage;

        public ChangeCollectionScopeCommandHandler(IContentManager contentList, IConnectApiHandler api,
            IUserSettingsStorage storage) {
            _contentList = contentList;
            _api = api;
            _storage = storage;
        }

        public async Task<Unit> Handle(ChangeCollectionScopeCommand request) {
            var collection = _contentList.CustomCollections.First(x => x.Id == request.Id);

            await collection.ChangeScope(_api, request.Scope).ConfigureAwait(false);

            await _storage.SaveNow().ConfigureAwait(false);
            return Unit.Value;
        }
    }
}