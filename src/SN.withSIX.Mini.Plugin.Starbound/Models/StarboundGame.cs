// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.Starbound.Models
{
    [Game(GameUUids.Starbound, Executables = new[] {@"win64\starbound.exe", @"win32\starbound.exe"}, Name = "Starbound",
        IsPublic = false,
        Slug = "Starbound")]
    [SynqRemoteInfo(GameUUids.Starbound)]
    [SteamInfo(SteamGameIds.Starbound)]
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

        protected override async Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var packages =
                launchContentAction.Content.Select(x => x.Content)
                    .OfType<IHavePackageName>()
                    .Select(x => x.PackageName)
                    .ToArray();

            HandleModDirectory(packages);
            //HandleSteamDirectory(packages);
        }

        private void HandleModDirectory(string[] packages) {
            var md = GetModInstallationDirectory();
            foreach (var f in md.DirectoryInfo.EnumerateFiles("*.pak"))
                HandleFileBasedMod(f, packages);
        }

        private static void HandleFileBasedMod(FileInfo f, string[] packages) {
            var pak = f.ToAbsoluteFilePath();
            var pakBak = pak.GetBrotherFileWithName(pak.FileNameWithoutExtension + ".bak");
            if (packages.Contains(pak.FileNameWithoutExtension)) {
                if (!pak.Exists && pakBak.Exists)
                    pakBak.Move(pak);
            } else {
                if (!pak.Exists)
                    return;
                if (pakBak.Exists)
                    pakBak.Delete();
                pak.Move(pakBak);
            }
        }

        private void HandleSteamDirectory(string[] packages) {
            foreach (var d in ContentPaths.Path.DirectoryInfo.EnumerateDirectories())
                HandleDirectoryBasedMod(d, packages);
        }

        private static void HandleDirectoryBasedMod(DirectoryInfo d, string[] packages) {
            var pak = d.ToAbsoluteDirectoryPath().GetChildFileWithName("contents.pak");
            var pakBak = d.ToAbsoluteDirectoryPath().GetChildFileWithName("contents.bak");
            if (packages.Contains(d.Name)) {
                if (!pak.Exists && pakBak.Exists)
                    pakBak.Move(pak);
            } else {
                if (!pak.Exists)
                    return;
                if (pakBak.Exists)
                    pakBak.Delete();
                pak.Move(pakBak);
            }
        }

        protected override string[] GetExecutables() => _executables.Value;

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory.GetChildDirectoryWithName("mods");

        protected override async Task InstallMod(IModContent mod) {
            var sourceDir = ContentPaths.Path.GetChildDirectoryWithName(mod.PackageName);
            var sourcePak = sourceDir.DirectoryInfo.EnumerateFiles("*.pak").First().ToAbsoluteFilePath();

            var installDirectory = GetModInstallationDirectory();
            installDirectory.MakeSurePathExists();

            var pakFile = installDirectory.GetChildFileWithName($"{mod.PackageName}.pak");
            await sourcePak.CopyAsync(pakFile).ConfigureAwait(false);
        }
    }
}