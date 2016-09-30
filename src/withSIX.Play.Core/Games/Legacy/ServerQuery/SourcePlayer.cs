// <copyright company="SIX Networks GmbH" file="SourcePlayer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy.Servers;

namespace withSIX.Play.Core.Games.Legacy.ServerQuery
{
    public class SourcePlayer : Player
    {
        public SourcePlayer(Server server, string name, int score, TimeSpan duration) : base(server, name) {
            Duration = duration;
            Score = score;
        }

        public int Score { get; }
        public TimeSpan Duration { get; }
    }
}