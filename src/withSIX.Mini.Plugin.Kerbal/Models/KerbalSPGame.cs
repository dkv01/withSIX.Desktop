// <copyright company="SIX Networks GmbH" file="KerbalSPGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using withSIX.Api.Models.Games;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Attributes;

namespace withSIX.Mini.Plugin.Kerbal.Models
{
    [Game(GameIds.KerbalSP, Name = "Kerbal Space Program", Executables = new[] {"ksp.exe"})]
    [SteamInfo(220200, "Kerbal Space Program")]
    [DataContract]
    public class KerbalSPGame : BasicGame
    {
        protected KerbalSPGame(Guid id) : this(id, new KerbalSPGameSettings()) {}
        public KerbalSPGame(Guid id, KerbalSPGameSettings settings) : base(id, settings) {}
    }
}