// <copyright company="SIX Networks GmbH" file="MakeContentFavorite.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Applications.Usecases.Main.Games
{
    public class MakeContentFavorite : IAsyncVoidCommand, IHaveId<Guid>, IHaveGameId
    {
        public MakeContentFavorite(Guid gameId, Guid id) {
            GameId = gameId;
            Id = id;
        }

        public Guid GameId { get; }
        public Guid Id { get; }
    }

    public class MakeContentFavoriteHandler : DbCommandBase, IAsyncVoidCommandHandler<MakeContentFavorite>
    {
        public MakeContentFavoriteHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<UnitType> HandleAsync(MakeContentFavorite request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var lc = game.Contents.FindOrThrowFromRequest(request);

            game.MakeFavorite(lc);

            return UnitType.Default;
        }
    }
}