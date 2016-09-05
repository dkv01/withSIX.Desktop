// <copyright company="SIX Networks GmbH" file="Homeworld2Game.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;
using SN.withSIX.Mini.Plugin.Homeworld.Services;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.Homeworld.Models
{
    [Game(GameIds.Homeworld2, Name = "Homeworld 2", Slug = "Homeworld-2", Executables = new[] {"homeworld2.exe"})]
    [RegistryInfo(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Homeworld2", "GAMEDIR")]
    [SynqRemoteInfo(GameIds.Homeworld2)]
    [DataContract]
    public class Homeworld2Game : BasicGame, ILaunchWith<IHomeworld2Launcher>
    {
        protected Homeworld2Game(Guid id) : this(id, new Homeworld2GameSettings()) {}
        public Homeworld2Game(Guid id, Homeworld2GameSettings settings) : base(id, settings) {}
    }
}