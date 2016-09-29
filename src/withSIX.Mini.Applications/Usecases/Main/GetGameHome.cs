// <copyright company="SIX Networks GmbH" file="GetGameHome.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Usecases.Main
{
    public class GetGameHome : IAsyncQuery<GameHomeApiModel>, IHaveId<Guid>
    {
        public GetGameHome(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class GetGameHomeHandler : DbQueryBase, IAsyncRequestHandler<GetGameHome, GameHomeApiModel>
    {
        public GetGameHomeHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<GameHomeApiModel> Handle(GetGameHome request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            return game.MapTo<GameHomeApiModel>();
        }
    }

    public class GameHomeApiModel : GameApiModelBase
    {
        public List<ContentApiModel> Recent { get; set; }
        public List<ContentApiModel> Favorites { get; set; }
        public List<ContentApiModel> NewContent { get; set; }
        public List<ContentApiModel> Updates { get; set; }
        public int InstalledModsCount { get; set; }
        public int InstalledMissionsCount { get; set; }
    }
}