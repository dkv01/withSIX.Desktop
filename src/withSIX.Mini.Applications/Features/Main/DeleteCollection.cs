// <copyright company="SIX Networks GmbH" file="DeleteCollection.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction]
    public class DeleteCollection : ICommand, IHaveId<Guid>, IHaveGameId
    {
        public DeleteCollection(Guid gameId, Guid id) {
            GameId = gameId;
            Id = id;
        }

        public Guid GameId { get; }
        public Guid Id { get; }
    }

    public class DeleteCollectionHandler : DbCommandBase, IAsyncRequestHandler<DeleteCollection>
    {
        public DeleteCollectionHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task Handle(DeleteCollection request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var findCollection = game.Collections.FindOrThrowFromRequest(request);
            game.RemoveCollection(findCollection);
        }
    }
}