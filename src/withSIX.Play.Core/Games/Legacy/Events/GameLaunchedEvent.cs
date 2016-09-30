// <copyright company="SIX Networks GmbH" file="GameLaunchedEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using withSIX.Play.Core.Connect.Events;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Core.Games.Legacy.Events
{
    public class GameLaunchedEvent : TimeStampedEvent
    {
        public RunningGame RunningGame { get; }
        public Server Server { get; }

        public GameLaunchedEvent(RunningGame runningGame, Server server = null) {
            Contract.Requires<ArgumentNullException>(runningGame != null);
            RunningGame = runningGame;
            Server = server;
        }
    }
}