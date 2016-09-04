// <copyright company="SIX Networks GmbH" file="GetHome.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Collections;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class GetHome : IAsyncQuery<HomeApiModel> {}

    public class GetHomeHandler : DbQueryBase, IAsyncRequestHandler<GetHome, HomeApiModel>
    {
        public GetHomeHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<HomeApiModel> Handle(GetHome request) {
            await GameContext.LoadAll().ConfigureAwait(false);
            var games =
                await GameContext.Games.Where(x => x.InstalledState.IsInstalled).ToListAsync().ConfigureAwait(false);

            return games.MapTo<HomeApiModel>();
        }
    }

    public class HomeApiModel
    {
        public List<GameApiModel> Games { get; set; }
        public List<ContentApiModel> NewContent { get; set; }
        public List<ContentApiModel> Updates { get; set; }
        public List<ContentApiModel> Recent { get; set; }
        public List<ContentApiModel> Favorites { get; set; }
    }

    public class ContentApiModel : ContentModel
    {
        public string Type { get; set; }
        public string GameSlug { get; set; } // TODO: Remove unneeded?
        public string Version { get; set; }
        public DateTime? UpdatedVersion { get; set; }
        public DateTime? LastUsed { get; set; }
        public DateTime? LastInstalled { get; set; }
        public DateTime? LastUpdated { get; set; }
        //public string InstalledVersion { get; set; }
        public bool IsFavorite { get; set; }
        public TypeScope TypeScope { get; set; }
        public bool IsNetworkContent { get; set; }
        public CollectionScope Scope { get; set; }
        public long Size { get; set; }
        public long SizePacked { get; set; }
    }

    public enum TypeScope
    {
        Local,
        Subscribed,
        Published
    }

    public abstract class GameApiModelBase
    {
        public Guid Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public Uri Image { get; set; }
    }

    public class GameApiModel : GameApiModelBase
    {
        public int CollectionsCount { get; set; }
        public int MissionsCount { get; set; }
        public int ModsCount { get; set; }
    }
}