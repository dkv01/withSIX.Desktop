// <copyright company="SIX Networks GmbH" file="SubGamesChanged.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Core.Games.Legacy.Events
{
    public class SubGamesChanged : IAsyncDomainEvent
    {
        public SubGamesChanged(Game gs) {
            Game = gs;
        }

        public Game Game { get; }
    }
}