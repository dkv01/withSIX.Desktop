﻿// <copyright company="SIX Networks GmbH" file="TakeOnMarsGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Api.Models.Games;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Attributes;

namespace withSIX.Mini.Plugin.Arma.Models
{
    [Game(GameIds.TKOM, Name = "Take on Mars", Slug = "Take-on-Mars", Executables = new[] {"tkom.exe"})]
    [SteamInfo(244030, "Take on Mars")]
    [RegistryInfo(@"SOFTWARE\Bohemia Interactive\Take On Mars", "main")]
    public class TakeOnMarsGame : BasicGame
    {
        protected TakeOnMarsGame(Guid id) : this(id, new TakeOnMarsGameSettings()) {}
        public TakeOnMarsGame(Guid id, TakeOnMarsGameSettings settings) : base(id, settings) {}
    }
}