﻿// <copyright company="SIX Networks GmbH" file="DayZGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using withSIX.Api.Models.Games;
using withSIX.Mini.Core.Games.Attributes;
using withSIX.Mini.Plugin.Arma.Attributes;

namespace withSIX.Mini.Plugin.Arma.Models
{
    [Game(GameIds.DayZSA, Name = "DayZ: Zombie RPG", Slug = "DayZ", Executables = new[] {"dayz.exe"})
    ]
    [SteamInfo(107410, "DayZ", DRM = true)]
    [SynqRemoteInfo(GameIds.DayZSA)]
    [RvProfileInfo("DayZ", "DayZ - other profiles", "DayZProfile")]
    [DataContract]
    public class DayZGame : RealVirtualityGame
    {
        protected DayZGame(Guid id) : this(id, new DayZGameSettings()) {}
        public DayZGame(Guid id, DayZGameSettings settings) : base(id, settings) {}
    }
}