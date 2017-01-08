// <copyright company="SIX Networks GmbH" file="GetGames.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    public class GetGames : IQuery<GamesApiModel> {}

    public class GetGamesHandler : DbQueryBase, IAsyncRequestHandler<GetGames, GamesApiModel>
    {
        public GetGamesHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<GamesApiModel> Handle(GetGames request) {
            await GameContext.LoadAll().ConfigureAwait(false);
            var games =
                await GameContext.Games
                    .Where(x => x.InstalledState.IsInstalled && (Consts.Features.UnreleasedGames || x.Metadata.IsPublic))
                    .ToListAsync()
                    .ConfigureAwait(false);
            return games.MapTo<GamesApiModel>();
        }
    }

    public class GamesApiModel
    {
        public List<GameApiModel> Games { get; set; }
    }
}