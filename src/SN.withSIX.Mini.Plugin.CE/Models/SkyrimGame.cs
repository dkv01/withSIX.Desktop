// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
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

namespace SN.withSIX.Mini.Plugin.CE.Models
{
    // SkyrimLauncher.exe
    [Game(GameUUids.Skyrim, Executables = new[] {@"SKSE.exe", @"TESV.exe"}, Name = "Skyrim",
        IsPublic = false,
        Slug = "Skyrim")]
    [SteamInfo(72850)]
    [DataContract]
    public class SkyrimGame : BasicSteamGame
    {
        public SkyrimGame(Guid id, SkyrimGameSettings settings) : base(id, settings) {}

        IAbsoluteDirectoryPath GetLocalAppDataFolder()
            => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                .ToAbsoluteDirectoryPath()
                .GetChildDirectoryWithName("Skyrim");

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory
                .GetChildDirectoryWithName("Data");


        protected override Task InstallMod(IModContent mod) {
            var m = new SkyrimMod(mod, ContentPaths.Path);
            return m.Install(GetModInstallationDirectory());
        }

        protected override async Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var pluginList = GetLocalAppDataFolder().GetChildFileWithName("plugins.txt");
            var loadOrder = GetLocalAppDataFolder().GetChildFileWithName("loadorder.txt");
            var contentList =
                new[] {"Skyrim.esm", "Update.esm"}.Concat(
                    launchContentAction.Content.Select(x => x.Content)
                        .OfType<IModContent>()
                        .Select(x => new SkyrimMod(x, ContentPaths.Path))
                        .Select(x => x.GetEsmFileName())
                        .Where(x => x != null))
                    .ToArray();
            // todo; backup and keep load order
            pluginList.WriteText(string.Join(Environment.NewLine, contentList));
            loadOrder.WriteText(string.Join(Environment.NewLine, contentList));
        }

        class SkyrimMod
        {
            private readonly IAbsoluteDirectoryPath _sourceDir;

            public SkyrimMod(IModContent mod, IAbsoluteDirectoryPath contentPath) {
                _sourceDir = contentPath.GetChildDirectoryWithName(mod.PackageName);
            }

            public string GetEsmFileName() {
                var sourceEsm = _sourceDir.DirectoryInfo.EnumerateFiles("*.esm").FirstOrDefault();
                var esm = sourceEsm?.ToAbsoluteFilePath();
                return esm?.FileName;
            }

            public async Task Install(IAbsoluteDirectoryPath installDirectory) {
                installDirectory.MakeSurePathExists();

                foreach (var f in _sourceDir.DirectoryInfo.EnumerateFiles())
                    await f.ToAbsoluteFilePath().CopyAsync(installDirectory).ConfigureAwait(false);
            }
        }
    }
}