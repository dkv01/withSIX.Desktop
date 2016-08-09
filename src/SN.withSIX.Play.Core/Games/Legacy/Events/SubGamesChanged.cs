// <copyright company="SIX Networks GmbH" file="SubGamesChanged.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core;
using SN.withSIX.Play.Core.Games.Entities;

namespace SN.withSIX.Play.Core.Games.Legacy.Events
{
    public class SubGamesChanged : IDomainEvent
    {
        public SubGamesChanged(Game gs) {
            Game = gs;
        }

        public Game Game { get; }
    }
}