// <copyright company="SIX Networks GmbH" file="NMSGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Runtime.Serialization;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.NMS.Models
{
    [Game(GameIds.NMS, Executables = new[] {@"Binaries\NMS.exe"}, Name = "No Mans Sky",
        IsPublic = false,
        Slug = "NMS")]
    [SynqRemoteInfo(GameIds.NMS)]
    [SteamInfo(SteamGameIds.NMS)]
    [DataContract]
    public class NMSGame : BasicGame
    {
        private readonly Lazy<string[]> _executables;

        public NMSGame(Guid id, NMSGameSettings settings) : base(id, settings) {
            _executables =
                new Lazy<string[]>(() => Environment.Is64BitOperatingSystem
                    ? Metadata.Executables
                    : Metadata.Executables.Skip(1).ToArray());
        }

        protected override bool ShouldLaunchWithSteam() => false;
    }
}