// <copyright company="SIX Networks GmbH" file="GameContentBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Usecases
{
    public abstract class GameContentBase : RequestBase, IHaveGameId, INeedGameContents, IHaveRequestName
    {
        protected GameContentBase(Guid gameId) {
            GameId = gameId;
        }

        public Guid GameId { get; }

        public string Name { get; set; }
        public Uri Href { get; set; }
    }

    public abstract class GameContentBaseWithInfo : GameContentBase
    {
        protected GameContentBaseWithInfo(Guid gameId) : base(gameId) {
            Name = "Playlist";
        }

        public Uri GetHref(Game game) => Href ?? GetHrefInternal(game);

        private Uri GetHrefInternal(Game game) => new Uri(Name == "Playlist"
            ? $"http://withsix.com/client-landing?gameSlug={game.Metadata.Slug}&openTab=playlist"
            : $"http://withsix.com/me/library/{game.Metadata.Slug}");
    }
}