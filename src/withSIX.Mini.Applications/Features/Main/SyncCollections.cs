// <copyright company="SIX Networks GmbH" file="SyncCollections.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features.Main
{
    [Obsolete("No longer used")]
    public class SyncCollections : ICommand, IHaveGameId
    {
        public SyncCollections(Guid gameId, List<ContentGuidSpec> contents) {
            GameId = gameId;
            Contents = contents;
        }

        public List<ContentGuidSpec> Contents { get; }
        public Guid GameId { get; }
    }

    public class SyncCollectionsHandler : DbCommandBase, IAsyncRequestHandler<SyncCollections>
    {
        readonly INetworkContentSyncer _contentSyncer;

        public SyncCollectionsHandler(IDbContextLocator dbContextLocator, INetworkContentSyncer contentSyncer)
            : base(dbContextLocator) {
            _contentSyncer = contentSyncer;
        }

        public async Task Handle(SyncCollections request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);

            await DealWithCollections(game, request.Contents).ConfigureAwait(false);
        }

        Task DealWithCollections(Game game, IEnumerable<ContentGuidSpec> contents)
            => DealWithCollections(game, contents, _contentSyncer);

        public static async Task DealWithCollections(Game game, IEnumerable<ContentGuidSpec> contents,
            INetworkContentSyncer networkContentSyncer) {
            var ids = contents.Select(x => x.Id).ToArray();
            var existingCollections = game.SubscribedCollections.Where(x => ids.Contains(x.Id)).ToList();
            var newCollectionIds =
                ids.Except(existingCollections.Select(x => x.Id)).ToList();

            await
                networkContentSyncer.SyncCollections(existingCollections).ConfigureAwait(false);
            if (newCollectionIds.Any()) {
                var newCollections =
                    await networkContentSyncer.GetCollections(game.Id, newCollectionIds).ConfigureAwait(false);
                game.Contents.AddRange(newCollections);
            }
        }
    }
}