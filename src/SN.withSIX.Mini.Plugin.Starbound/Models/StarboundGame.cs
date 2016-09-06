// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MoreLinq;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.Starbound.Models
{
    [Game(GameIds.Starbound,
        Executables = new[] {@"win64\starbound.exe", @"win32\starbound.exe"},
        ServerExecutables = new[] { @"win64\starbound_server.exe", @"win32\starbound_server.exe" },
        Name = "Starbound",
        IsPublic = true,
        Slug = "Starbound")]
    [SynqRemoteInfo(GameIds.Starbound)]
    [SteamInfo(SteamGameIds.Starbound, "Starbound")]
    [DataContract]
    public class StarboundGame : BasicSteamGame
    {
        private readonly Lazy<IRelativeFilePath[]> _executables;
        private readonly Lazy<IRelativeFilePath[]> _serverExecutables;

        public StarboundGame(Guid id, StarboundGameSettings settings) : base(id, settings) {
            _executables =
                new Lazy<IRelativeFilePath[]>(() => Environment.Is64BitOperatingSystem
                    ? Metadata.GetExecutables().ToArray()
                    : Metadata.GetExecutables().Skip(1).ToArray());
            _serverExecutables =
                new Lazy<IRelativeFilePath[]>(() => Environment.Is64BitOperatingSystem
                    ? Metadata.GetServerExecutables().ToArray()
                    : Metadata.GetServerExecutables().Skip(1).ToArray());
        }

        protected override bool ShouldLaunchWithSteam(LaunchState ls) => false;

        protected override Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            // TODO: PublisherId

            var content = launchContentAction.Content.SelectMany(x => x.Content.GetLaunchables(x.Constraint)).ToArray();
            var packages = content.OfType<IHavePackageName>()
                .Select(x => x.PackageName)
                .Distinct()
                .ToArray();
            HandleModDirectory(packages);

            return EnableModsInternal(content.OfType<IModContent>().Select(CreateMod), m => m.Enable());
        }

        protected override IEnumerable<IRelativeFilePath> GetExecutables(LaunchAction action) =>
            action == LaunchAction.LaunchAsDedicatedServer ? _serverExecutables.Value : _executables.Value;

        protected override IAbsoluteDirectoryPath GetDefaultDirectory()
            => GetGogDir("Starbound") ?? base.GetDefaultDirectory();

        private void HandleModDirectory(string[] packages) {
            var md = GetModInstallationDirectory();
            foreach (var f in md.DirectoryInfo.EnumerateFiles("*.pak"))
                HandleFileBasedMod(f, packages);
        }

        private static void HandleFileBasedMod(FileInfo f, IEnumerable<string> packages) {
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

        //private static readonly string[] defaultStartupParameters = {"-noworkshop"};

        //protected override IEnumerable<string> GetStartupParameters() => defaultStartupParameters.Concat(base.GetStartupParameters());

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory.GetChildDirectoryWithName("mods");

        protected override Task InstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Install(true);
        }

        protected override Task UninstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Uninstall();
        }

        private StarboundMod CreateMod(IModContent mod)
            =>
                new StarboundMod(mod, GetContentSourceDirectory(mod), GetModInstallationDirectory(),
                    InstalledState.Directory);

        public override Uri GetPublisherUrl(ContentPublisher c) {
            switch (c.Publisher) {
            case Publisher.Chucklefish:
                return new Uri(GetPublisherUrl(Publisher.Chucklefish), $"{c.PublisherId}");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c.Publisher} for this game");
        }

        public override Uri GetPublisherUrl(Publisher c) {
            switch (c) {
            case Publisher.Chucklefish:
                return new Uri($"http://community.playstarbound.com/resources/");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c} for this game");
        }

        public override Uri GetPublisherUrl() => GetPublisherUrl(Publisher.Chucklefish);

        class StarboundMod : SteamMod
        {
            private readonly IAbsoluteDirectoryPath _gameDir;
            private readonly IAbsoluteDirectoryPath _modDir;

            public StarboundMod(IModContent mod, IAbsoluteDirectoryPath sourceDir, IAbsoluteDirectoryPath modDir,
                IAbsoluteDirectoryPath gameDir) : base(sourceDir, mod) {
                _modDir = modDir;
                _gameDir = gameDir;
            }

            public Task Enable() => Install(false);

            protected override async Task InstallImpl(bool force) {
                _modDir.MakeSurePathExists();
                var exts = new[] { ".pak", ".modpak" };
                var destPakFile = _modDir.GetChildFileWithName($"{Mod.PackageName}.pak");
                if (!force && destPakFile.Exists) // TODO: Date check
                    return;
                
                // TODO: Support mods without Paks, as folder ? Or mark as not-installable
                var sourcePakFiles =
                    exts.SelectMany(x => SourcePath.DirectoryInfo.EnumerateFiles($"*{x}", SearchOption.AllDirectories))
                        .Select(x => x.ToAbsoluteFilePath()).ToArray();
                var sourcePak = sourcePakFiles.FirstOrDefault();
                IAbsoluteFilePath sourcePakPath;
                if (sourcePak == null || !sourcePak.Exists) {
                    var modInfo =
                        SourcePath.DirectoryInfo.EnumerateFiles("*.modinfo", SearchOption.AllDirectories)
                            .FirstOrDefault();
                    if (modInfo != null)
                        sourcePakPath = await PackModInfoMod(modInfo.ToAbsoluteFilePath()).ConfigureAwait(false);
                    else {
                        var metadata =
                            SourcePath.DirectoryInfo.EnumerateFiles(".metadata", SearchOption.AllDirectories)
                                .FirstOrDefault();
                        if (metadata == null) {
                            throw new NotInstallableException(
                                $"{Mod.PackageName} source .pak not found! You might try Diagnosing");
                        }
                        sourcePakPath = await PackMetadataMod(metadata.ToAbsoluteFilePath()).ConfigureAwait(false);
                    }
                } else
                    sourcePakPath = sourcePak;
                await sourcePakPath.CopyAsync(destPakFile).ConfigureAwait(false);
            }

            private async Task<IAbsoluteFilePath> PackModInfoMod(IAbsoluteFilePath modInfoPath) {
                await modInfoPath.CopyAsync(modInfoPath.GetBrotherFileWithName("pak.modinfo")).ConfigureAwait(false);
                return await CreatePakFile(modInfoPath.ParentDirectoryPath).ConfigureAwait(false);
            }

            private Task<IAbsoluteFilePath> PackMetadataMod(IAbsoluteFilePath metadataPath)
                => CreatePakFile(metadataPath.ParentDirectoryPath);

            private async Task<IAbsoluteFilePath> CreatePakFile(IAbsoluteDirectoryPath sourceDir) {
                var sourcePakPath =
                    Path.GetTempPath().ToAbsoluteDirectoryPath().GetChildFileWithName($"{Mod.PackageName}.pak");
                // TODO: Delete after usage
                var r = await
                    Tools.ProcessManager.LaunchAndGrabAsync(
                        new BasicLaunchInfo(new ProcessStartInfo(
                            _gameDir.GetChildFileWithName(@"win32\asset_packer.exe").ToString(),
                            new[] {sourceDir.ToString(), sourcePakPath.ToString()}
                                .CombineParameters()))).ConfigureAwait(false);
                if (r.ExitCode != 0) {
                    throw new Exception(
                        $"Failed creating a pak file for the mod. Code: {r.ExitCode} Output: {r.StandardOutput} Error: {r.StandardError}");
                }
                return sourcePakPath;
            }

            protected override async Task UninstallImpl() {
                if (!_modDir.Exists)
                    return;
                new[] {
                    _modDir.GetChildFileWithName($"{Mod.PackageName}.pak"),
                    _modDir.GetChildFileWithName($"{Mod.PackageName}.modpak")
                }.Where(x => x.Exists).ForEach(x => x.Delete());
            }
        }
    }
}