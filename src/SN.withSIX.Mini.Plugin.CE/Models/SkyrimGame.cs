// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using SN.withSIX.Core;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;

namespace SN.withSIX.Mini.Plugin.CE.Models
{
    // SkyrimLauncher.exe
    [Game(GameUUids.Skyrim, Executables = new[] { @"SKSE.exe", @"TESV.exe" }, Name = "Skyrim",
        IsPublic = false,
        Slug = "Skyrim")]
    [SteamInfo(72850)]
    [DataContract]
    public class SkyrimGame : BasicSteamGame
    {
        public SkyrimGame(Guid id, SkyrimGameSettings settings) : base(id, settings) {}
    }
}