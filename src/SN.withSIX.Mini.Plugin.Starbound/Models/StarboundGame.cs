// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Mini.Plugin.Starbound.Models
{
    [Game(GameUUids.Starbound, Executables = new[] {@"win64\starbound.exe", @"win32\starbound.exe"}, Name = "Starbound",
        IsPublic = false,
        Slug = "Starbound")]
    [SteamInfo(211820)]
    [DataContract]
    public class StarboundGame : BasicSteamGame
    {
        private readonly Lazy<string[]> _executables;

        public StarboundGame(Guid id, StarboundGameSettings settings) : base(id, settings) {
            _executables =
                new Lazy<string[]>(() => Environment.Is64BitOperatingSystem
                    ? Metadata.Executables
                    : Metadata.Executables.Skip(1).ToArray());
        }

        protected override async Task<Process> LaunchImpl(IGameLauncherFactory factory,
            ILaunchContentAction<IContent> launchContentAction) {
            await EnableMods(launchContentAction).ConfigureAwait(false);
            return await base.LaunchImpl(factory, launchContentAction).ConfigureAwait(false);
        }

        private async Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var packages =
                launchContentAction.Content.Select(x => x.Content)
                    .OfType<IHavePackageName>()
                    .Select(x => x.PackageName)
                    .ToArray();
            foreach (var d in ContentPaths.Path.DirectoryInfo.EnumerateDirectories()) {
                var pak = d.ToAbsoluteDirectoryPath().GetChildFileWithName("contents.pak");
                var pakBak = d.ToAbsoluteDirectoryPath().GetChildFileWithName("contents.bak");
                if (packages.Contains(d.Name)) {
                    if (!pak.Exists && pakBak.Exists)
                        pakBak.Move(pak);
                } else {
                    if (!pak.Exists)
                        continue;
                    if (pakBak.Exists)
                        pakBak.Delete();
                    pak.Move(pakBak);
                }
            }
        }

        protected override string[] GetExecutables() => _executables.Value;
        /*
                IAbsoluteDirectoryPath GetModInstallationDirectory() => InstalledState.Directory.GetChildDirectoryWithName("mods");
                protected override async Task InstallMod(IModContent mod) {
                    var sourceDir = ContentPaths.Path.GetChildDirectoryWithName(mod.PackageName);
                    var sourcePak = sourceDir.DirectoryInfo.EnumerateFiles("*.pak").First().ToAbsoluteFilePath();

                    var installDirectory = GetModInstallationDirectory();
                    installDirectory.MakeSurePathExists();

                    var pakFile = installDirectory.GetChildFileWithName($"{mod.PackageName}.pak");
                    await sourcePak.CopyAsync(pakFile).ConfigureAwait(false);
                }
            */
    }
}