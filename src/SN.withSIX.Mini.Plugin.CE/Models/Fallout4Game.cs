// <copyright company="SIX Networks GmbH" file="SkyrimGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.CE.Models
{
    // TODO: SKSE?
    [Game(GameIds.Fallout4, Executables = new[] {@"Fallout4.exe"}, Name = "Fallout 4",
        IsPublic = false,
        Slug = "Fallout4")]
    // [SynqRemoteInfo(GameIds.Fallout4)] // TODO
    [SteamInfo(SteamGameIds.Fallout4, "Fallout 4")]
    [DataContract]
    public class Fallout4Game : BasicSteamGame
    {
        public Fallout4Game(Guid id, Fallout4GameSettings settings) : base(id, settings) {}

        IAbsoluteDirectoryPath GetLocalAppDataFolder()
            => PathConfiguration.GetFolderPath(EnvironmentSpecial.SpecialFolder.LocalApplicationData)
                .ToAbsoluteDirectoryPath()
                .GetChildDirectoryWithName("Fallout4");

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory
                .GetChildDirectoryWithName("Data");

        protected override IEnumerable<IAbsoluteDirectoryPath> GetModFolders() {
            if (ContentPaths.IsValid)
                yield return ContentPaths.Path;
        }


        protected override Task InstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Install();
        }

        protected override Task UninstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Uninstall();
        }

        protected override Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var pluginList = GetLocalAppDataFolder().GetChildFileWithName("plugins.txt");
            var loadOrder = GetLocalAppDataFolder().GetChildFileWithName("loadorder.txt");
            var contentList =
                new[] {"Fallout4.esm", "Update.esm"}.Concat(
                    launchContentAction.Content.Select(x => x.Content)
                        .OfType<IModContent>()
                        .Select(CreateMod)
                        .Select(x => x.GetEsmFileName())
                        .Where(x => x != null))
                    .ToArray();
            // todo; backup and keep load order
            pluginList.WriteText(string.Join(Environment.NewLine, contentList));
            loadOrder.WriteText(string.Join(Environment.NewLine, contentList));
            return TaskExt.Default;
        }

        private Fallout4Mod CreateMod(IModContent x)
            => new Fallout4Mod(GetContentSourceDirectory(x), GetModInstallationDirectory());

        public override Uri GetPublisherUrl(ContentPublisher c) {
            switch (c.Publisher) {
                case Publisher.NexusMods:
                    return new Uri(GetPublisherUrl(c.Publisher), $"{c.PublisherId}/?");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c.Publisher} for this game");
        }

        public override Uri GetPublisherUrl(Publisher c) {
            switch (c) {
                case Publisher.NexusMods:
                    return new Uri($"http://www.nexusmods.com/fallout4/mods/");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c} for this game");
        }

        public override Uri GetPublisherUrl() => GetPublisherUrl(Publisher.NexusMods);

        // TODO: Subdirectories support
        class Fallout4Mod
        {
            private readonly IAbsoluteDirectoryPath _installPath;
            private readonly IAbsoluteDirectoryPath _sourceDir;

            public Fallout4Mod(IAbsoluteDirectoryPath contentPath, IAbsoluteDirectoryPath installPath) {
                _sourceDir = contentPath;
                _installPath = installPath;
            }

            public string GetEsmFileName() {
                var sourceEsm = _sourceDir.DirectoryInfo.EnumerateFiles("*.esm").FirstOrDefault();
                var esm = sourceEsm?.ToAbsoluteFilePath();
                return esm?.FileName;
            }

            public async Task Install() {
                _installPath.MakeSurePathExists();

                foreach (var f in _sourceDir.DirectoryInfo.EnumerateFiles())
                    await f.ToAbsoluteFilePath().CopyAsync(_installPath).ConfigureAwait(false);
            }

            public Task Uninstall() {
                if (!_installPath.Exists || !_sourceDir.Exists)
                    return TaskExt.Default;
                foreach (var df in
                    _sourceDir.DirectoryInfo.EnumerateFiles()
                        .Select(f => _installPath.GetChildFileWithName(f.Name))
                        .Where(df => df.Exists))
                    df.Delete();

                return TaskExt.Default;
            }
        }
    }
}