// <copyright company="SIX Networks GmbH" file="NMSGame.cs">
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
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.NMS.Models
{
    [Game(GameIds.NMS, Executables = new[] {@"Binaries\NMS.exe"}, Name = "No Mans Sky",
        IsPublic = false,
        Slug = "NMS")]
    [SynqRemoteInfo(GameIds.NMS)]
    [SteamInfo(SteamGameIds.NMS)]
    [DataContract]
    public class NMSGame : BasicSteamGame
    {
        public NMSGame(Guid id, NMSGameSettings settings) : base(id, settings) {}

        protected override Task InstallMod(IModContent mod) => CreateMod(mod).Install(true);

        private NMSMod CreateMod(IModContent mod)
            => new NMSMod(mod, GetContentSourceDirectory(mod), GetModInstallationDirectory());

        protected override Task UninstallMod(IModContent mod) => CreateMod(mod).Uninstall();

        protected override async Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var content = launchContentAction.Content.SelectMany(x => x.Content.GetLaunchables(x.Constraint)).ToArray();
            var packages = content.OfType<IHavePackageName>()
                .Select(x => x.PackageName)
                .Distinct()
                .ToArray();
            HandleModDirectory(packages);

            foreach (var m in content.OfType<IModContent>().Select(CreateMod))
                await m.Install(false).ConfigureAwait(false);
        }

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory.GetChildDirectoryWithName("mods");

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
    }

    class NMSMod
    {
        private readonly IAbsoluteDirectoryPath _destination;
        private readonly IModContent _mod;
        private readonly IAbsoluteDirectoryPath _source;

        public NMSMod(IModContent mod, IAbsoluteDirectoryPath source, IAbsoluteDirectoryPath destination) {
            _mod = mod;
            _source = source;
            _destination = destination;
        }

        public async Task Install(bool force) {
            _destination.MakeSurePathExists();
            var pakFile = _destination.GetChildFileWithName(GetPakName());
            if (!force && pakFile.Exists) // TODO: Date check
                return;
            if (!_source.Exists)
                throw new NotFoundException($"{_mod.PackageName} source not found! You might try Diagnosing");
            var sourcePak = _source.DirectoryInfo.EnumerateFiles("*.pak").First().ToAbsoluteFilePath();
            if (!sourcePak.Exists)
                throw new NotFoundException($"{_mod.PackageName} source .pak not found! You might try Diagnosing");
            await sourcePak.CopyAsync(pakFile).ConfigureAwait(false);
        }

        private string GetPakName() => $"{_mod.PackageName}.pak";

        public async Task Uninstall() {
            if (!_destination.Exists)
                return;

            var pakFile = _destination.GetChildFileWithName(GetPakName());
            if (pakFile.Exists)
                pakFile.Delete();
        }
    }
}