﻿// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.Starbound.Models
{
    [Game(GameIds.Starbound, Executables = new[] {@"win64\starbound.exe", @"win32\starbound.exe"}, Name = "Starbound",
        IsPublic = true,
        Slug = "Starbound")]
    [SynqRemoteInfo(GameIds.Starbound)]
    [SteamInfo(SteamGameIds.Starbound, "Starbound")]
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

        protected override bool ShouldLaunchWithSteam() => false;

        protected override Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            // TODO: PublisherId

            var content = launchContentAction.Content.SelectMany(x => x.Content.GetLaunchables(x.Constraint)).ToArray();
            var packages = content.OfType<IHavePackageName>()
                .Select(x => x.PackageName)
                .Distinct()
                .ToArray();
            HandleModDirectory(packages);

            return EnableModsInternal(content.OfType<IModContent>().Select(CreateMod), m => m.Install(false));
        }

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

        protected override string[] GetExecutables() => _executables.Value;

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
    }

    internal class StarboundMod
    {
        private readonly IAbsoluteDirectoryPath _gameDir;
        private readonly IModContent _mod;
        private readonly IAbsoluteDirectoryPath _modDir;
        private readonly IAbsoluteDirectoryPath _sourceDir;

        public StarboundMod(IModContent mod, IAbsoluteDirectoryPath sourceDir, IAbsoluteDirectoryPath modDir,
            IAbsoluteDirectoryPath gameDir) {
            _mod = mod;
            _sourceDir = sourceDir;
            _modDir = modDir;
            _gameDir = gameDir;
        }

        public async Task Install(bool force) {
            _modDir.MakeSurePathExists();
            var pakFile = _modDir.GetChildFileWithName($"{_mod.PackageName}.pak");
            if (!force && pakFile.Exists) // TODO: Date check
                return;
            if (!_sourceDir.Exists)
                throw new NotFoundException($"{_mod.PackageName} source not found! You might try Diagnosing");
            // TODO: Support mods without Paks, perhaps as "not installable"

            // 1. rename *.modinfo to pak.modinfo
            var c = @"win32\asset_packer.exe modfolder modfolder.pak";

            var sourcePak =
                _sourceDir.DirectoryInfo.EnumerateFiles("*.pak", SearchOption.AllDirectories).FirstOrDefault();
            IAbsoluteFilePath sourcePakPath;
            if (sourcePak == null || !sourcePak.Exists) {
                var modInfo =
                    _sourceDir.DirectoryInfo.EnumerateFiles("*.modinfo", SearchOption.AllDirectories).FirstOrDefault();
                if (modInfo == null) {
                    throw new NotInstallableException(
                        $"{_mod.PackageName} source .pak not found! You might try Diagnosing");
                }
                // todo get the dir
                var modInfoPath = modInfo.ToAbsoluteFilePath();
                sourcePakPath = await PackMod(modInfoPath).ConfigureAwait(false);
            } else
                sourcePakPath = sourcePak.ToAbsoluteFilePath();
            await sourcePakPath.CopyAsync(pakFile).ConfigureAwait(false);
        }

        private async Task<IAbsoluteFilePath> PackMod(IAbsoluteFilePath modInfoPath) {
            await modInfoPath.CopyAsync(modInfoPath.GetBrotherFileWithName("pak.modinfo")).ConfigureAwait(false);
            var sourcePakPath =
                Path.GetTempPath().ToAbsoluteDirectoryPath().GetChildFileWithName($"{_mod.PackageName}.pak");
            // TODO: Delete after usage
            var r = await
                Tools.ProcessManager.LaunchAndGrabAsync(
                    new BasicLaunchInfo(new ProcessStartInfo(
                        _gameDir.GetChildFileWithName(@"win32\asset_packer.exe").ToString(),
                        new[] {modInfoPath.ParentDirectoryPath.ToString(), sourcePakPath.ToString()}
                            .CombineParameters()))).ConfigureAwait(false);
            if (r.ExitCode != 0)
                throw new Exception(
                    $"Failed creating a pak file for the mod. Code: {r.ExitCode} Output: {r.StandardOutput} Error: {r.StandardError}");
            return sourcePakPath;
        }

        public async Task Uninstall() {
            if (!_modDir.Exists)
                return;

            var pakFile = _modDir.GetChildFileWithName($"{_mod.PackageName}.pak");
            if (pakFile.Exists)
                pakFile.Delete();
        }
    }
}