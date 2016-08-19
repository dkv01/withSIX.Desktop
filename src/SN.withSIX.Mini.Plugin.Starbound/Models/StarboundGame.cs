// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
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
            // TODO: PublisherId
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

        private static readonly string[] defaultStartupParameters = {"-noworkshop"};

        protected override IEnumerable<string> GetStartupParameters()
            => defaultStartupParameters.Concat(base.GetStartupParameters());

        protected override string[] GetExecutables() => _executables.Value;

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory.GetChildDirectoryWithName("mods");

        protected override Task InstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Install();
        }

        protected override Task UninstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Uninstall();
        }

        private StarboundMod CreateMod(IModContent mod)
            => new StarboundMod(mod, GetContentSourceDirectory(mod), GetModInstallationDirectory());
    }

    internal class StarboundMod
    {
        private readonly IModContent _mod;
        private readonly IAbsoluteDirectoryPath _modDir;
        private readonly IAbsoluteDirectoryPath _sourceDir;

        public StarboundMod(IModContent mod, IAbsoluteDirectoryPath sourceDir, IAbsoluteDirectoryPath modDir) {
            _mod = mod;
            _sourceDir = sourceDir;
            _modDir = modDir;
        }

        public async Task Install() {
            var sourcePak = _sourceDir.DirectoryInfo.EnumerateFiles("*.pak").First().ToAbsoluteFilePath();

            _modDir.MakeSurePathExists();

            var pakFile = _modDir.GetChildFileWithName($"{_mod.PackageName}.pak");
            await sourcePak.CopyAsync(pakFile).ConfigureAwait(false);
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