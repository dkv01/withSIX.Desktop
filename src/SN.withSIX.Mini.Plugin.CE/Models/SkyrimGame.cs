// <copyright company="SIX Networks GmbH" file="SkyrimGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.CE.Models
{
    // SkyrimLauncher.exe
    [Game(GameIds.Skyrim, Executables = new[] {@"SKSE.exe", @"TESV.exe"}, Name = "Skyrim",
        IsPublic = true,
        Slug = "Skyrim")]
    [SynqRemoteInfo(GameIds.Skyrim)]
    [SteamInfo(SteamGameIds.Skyrim, "Skyrim")]
    [DataContract]
    public class SkyrimGame : BasicSteamGame
    {
        public SkyrimGame(Guid id, SkyrimGameSettings settings) : base(id, settings) {}

        IAbsoluteDirectoryPath GetLocalAppDataFolder()
            => PathConfiguration.GetFolderPath(EnvironmentSpecial.SpecialFolder.LocalApplicationData)
                .ToAbsoluteDirectoryPath()
                .GetChildDirectoryWithName("Skyrim");

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory
                .GetChildDirectoryWithName("Data");

        protected override Task InstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Install(true);
        }

        protected override Task UninstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Uninstall();
        }

        protected override Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var pluginList = GetLocalAppDataFolder().GetChildFileWithName("plugins.txt");
            var loadOrder = GetLocalAppDataFolder().GetChildFileWithName("loadorder.txt");
            var contentList =
                new[] {"Skyrim.esm", "Update.esm"}.Concat(
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

        private SkyrimMod CreateMod(IModContent x)
            => new SkyrimMod(GetContentSourceDirectory(x), GetModInstallationDirectory(), x);

        // TODO: Subdirectories support
        class SkyrimMod : SteamMod
        {
            private readonly IAbsoluteDirectoryPath _installPath;

            public SkyrimMod(IAbsoluteDirectoryPath contentPath, IAbsoluteDirectoryPath installPath, IModContent mod)
                : base(contentPath, mod) {
                _installPath = installPath;
            }

            public string GetEsmFileName() {
                var sourceEsm = SourcePath.DirectoryInfo.EnumerateFiles("*.esm").FirstOrDefault();
                var esm = sourceEsm?.ToAbsoluteFilePath();
                return esm?.FileName;
            }

            protected override async Task InstallImpl(bool force) {
                _installPath.MakeSurePathExists();

                foreach (var f in SourcePath.DirectoryInfo.EnumerateFiles())
                    await f.ToAbsoluteFilePath().CopyAsync(_installPath).ConfigureAwait(false);
            }

            protected override async Task UninstallImpl() {
                if (!_installPath.Exists || !SourcePath.Exists)
                    return;
                foreach (var df in
                    SourcePath.DirectoryInfo.EnumerateFiles()
                        .Select(f => _installPath.GetChildFileWithName(f.Name))
                        .Where(df => df.Exists))
                    df.Delete();
            }
        }
    }
}