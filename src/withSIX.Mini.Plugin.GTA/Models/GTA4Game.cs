// <copyright company="SIX Networks GmbH" file="GTA4Game.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NDepend.Path;
using withSIX.Api.Models.Games;
using withSIX.Core.Extensions;
using withSIX.Mini.Core.Games.Attributes;
using withSIX.Mini.Core.Games.Services.ContentInstaller.Attributes;

namespace withSIX.Mini.Plugin.GTA.Models
{
    [Game(GameIds.GTAIV, Name = "GTA 4", Slug = "GTA-4",
         Executables = new[] {"GTAIV\\GTAIV.exe"})]
    //[GTA4ContentCleaning]
    [SteamInfo(SteamGameIds.GTA4, "Grand Theft Auto 4")]
    [SynqRemoteInfo(GameIds.GTAIV)]
    [RegistryInfo(@"Rockstar Games\Grand Theft Auto IV", "InstallFolder")]
    [DataContract]
    public class GTA4Game : GTAGame
    {
        protected GTA4Game(Guid id) : this(id, new GTA4GameSettings()) {}
        public GTA4Game(Guid id, GTA4GameSettings settings) : base(id, settings) {}
    }

    public class GTA4ContentCleaningAttribute : ContentCleaningAttribute
    {
        public override IReadOnlyCollection<IRelativePath> Exclusions => GameFiles();
        // TODO
        static IReadOnlyCollection<IRelativeFilePath> GameFiles()
            => new string[0].ToRelativeFilePaths().ToList();
    }
}