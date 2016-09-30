// <copyright company="SIX Networks GmbH" file="ActiveGameChanged.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Core.Games.Legacy.Events
{
    public class ActiveGameChanged : EventArgs, IAsyncDomainEvent
    {
        public Game Game { get; }

        public ActiveGameChanged(Game game) {
            Game = game;
        }
    }
}