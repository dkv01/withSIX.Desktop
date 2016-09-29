// <copyright company="SIX Networks GmbH" file="PreGameLaunchCancelleableEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Play.Core.Connect.Events;
using SN.withSIX.Play.Core.Games.Entities;
using SN.withSIX.Play.Core.Games.Legacy.Missions;
using SN.withSIX.Play.Core.Games.Legacy.Mods;

namespace SN.withSIX.Play.Core.Games.Legacy.Events
{
    public class PreGameLaunchCancelleableEvent : CancellableEvent
    {
        public PreGameLaunchCancelleableEvent(Game game, Collection collection, MissionBase mission, Server server) {
            Game = game;
            Collection = collection;
            Mission = mission;
            Server = server;
        }

        public Server Server { get; }
        public MissionBase Mission { get; }
        public Collection Collection { get; }
        public Game Game { get; }
    }
}