﻿// <copyright company="SIX Networks GmbH" file="NMSGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.NMS.Models
{
    // TODO: Registry, but also auto detection scanner..
    [Game(GameIds.NMS, Executables = new[] {@"Binaries\NMS.exe"}, Name = "No Man's Sky",
        IsPublic = true,
        Slug = "NoMansSky")]
    //[SynqRemoteInfo(GameIds.NMS)] // TODO
    [SteamInfo(SteamGameIds.NMS, "No Man's Sky")]
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
            => InstalledState.Directory.GetChildDirectoryWithName(@"GAMEDATA\PCBANKS");

        private void HandleModDirectory(string[] packages) {
            var md = GetModInstallationDirectory();
            foreach (var f in md.DirectoryInfo.EnumerateFiles("_*.pak"))
                HandleFileBasedMod(f, packages);
        }

        protected override IEnumerable<IAbsoluteDirectoryPath> GetModFolders() {
            if (ContentPaths.IsValid)
                yield return ContentPaths.Path;
        }

        protected override IAbsoluteDirectoryPath GetDefaultDirectory() => GetGogDir("No Man's Sky") ?? base.GetDefaultDirectory();

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

        public override Uri GetPublisherUrl(ContentPublisher c) {
            switch (c.Publisher) {
            case Publisher.NoMansSkyMods:
                return new Uri(GetPublisherUrl(Publisher.NoMansSkyMods), $"{c.PublisherId}");
            case Publisher.NexusMods:
                return new Uri(GetPublisherUrl(Publisher.NexusMods), $"{c.PublisherId}/?");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c.Publisher} for this game");
        }

        public override Uri GetPublisherUrl(Publisher c) {
            switch (c) {
                case Publisher.NoMansSkyMods:
                    return new Uri($"http://nomansskymods.com/mods/");
                case Publisher.NexusMods:
                    return new Uri($"http://www.nexusmods.com/nomanssky/mods/");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c} for this game");
        }

        public override Uri GetPublisherUrl() => GetPublisherUrl(Publisher.NoMansSkyMods);
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

            foreach (var c in _source.DirectoryInfo.EnumerateFiles("*")
                .Where(x => NDependPathHelpers.ArchiveRx.IsMatch(x.Extension))
                .Select(x => x.ToAbsoluteFilePath()))
                c.Unpack(_source, true);

            // TODO: Or each included?
            var sourcePak =
                _source.DirectoryInfo.EnumerateFiles("*.pak", SearchOption.AllDirectories).First().ToAbsoluteFilePath();
            if (!sourcePak.Exists)
                throw new NotFoundException($"{_mod.PackageName} source .pak not found! You might try Diagnosing");
            await sourcePak.CopyAsync(pakFile).ConfigureAwait(false);
        }

        private string GetPakName() => $"{GetPrefix()}{_mod.PackageName}.pak";

        private string GetPrefix() => _mod.PackageName.StartsWith("_") ? "" : "_";

        public async Task Uninstall() {
            if (!_destination.Exists)
                return;

            var pakFile = _destination.GetChildFileWithName(GetPakName());
            if (pakFile.Exists)
                pakFile.Delete();
        }
    }
}