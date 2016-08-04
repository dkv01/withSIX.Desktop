// <copyright company="SIX Networks GmbH" file="StellarisGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;

namespace SN.withSIX.Mini.Plugin.Stellaris.Models
{
    [Game(GameUUids.Stellaris, Executables = new[] {@"stellaris.exe"}, Name = "Stellaris", IsPublic = false,
        Slug = "Stellaris")]
    [SynqRemoteInfo(GameUUids.Stellaris)]
    [SteamInfo(281990)]
    [DataContract]
    public class StellarisGame : BasicSteamGame
    {
        private const string ModStart = "last_mods={";
        private const string ModEnd = "}";
        public StellarisGame(Guid id, StellarisGameSettings settings) : base(id, settings) {}

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => GetDocumentsDirectory().GetChildDirectoryWithName("mod");

        private static IAbsoluteDirectoryPath GetDocumentsDirectory()
            => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                .ToAbsoluteDirectoryPath()
                .GetChildDirectoryWithName(@"Paradox Interactive\Stellaris");

        protected override async Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var sf = GetDocumentsDirectory().GetChildFileWithName("settings.txt");
            var settings = GetSettingsWithoutMods(sf);
            WriteNewModsSection(launchContentAction, settings);
            sf.WriteText(settings.ToString());
        }

        private static StringBuilder GetSettingsWithoutMods(IAbsoluteFilePath sf) {
            var settings = new StringBuilder();
            var settingsLines = sf.ReadLines();
            var started = false;
            var ended = false;
            foreach (var l in settingsLines) {
                if (!started) {
                    if (!l.StartsWith(ModStart))
                        settings.AppendLine(l);
                    else
                        started = true;
                } else if (ended)
                    settings.AppendLine(l);
                else if (l == ModEnd)
                    ended = true;
            }
            return settings;
        }

        private void WriteNewModsSection(ILaunchContentAction<IContent> launchContentAction, StringBuilder settings) {
            settings.AppendLine(ModStart);
            foreach (
                var m in
                    launchContentAction.Content.Select(x => x.Content).OfType<IModContent>().Select(CreateStellarisMod))
                settings.AppendLine($"\t\"{m.GetRelModName()}.mod\"");
            settings.AppendLine(ModEnd);
        }

        protected override Task InstallMod(IModContent mod) {
            var m = CreateStellarisMod(mod);
            return m.InstallMod();
        }

        private StellarisMod CreateStellarisMod(IModContent x)
            => new StellarisMod(x, ContentPaths.Path, GetModInstallationDirectory());


        class StellarisMod
        {
            private readonly IAbsoluteDirectoryPath _modPath;
            private readonly IAbsoluteDirectoryPath _sourcePath;
            private readonly Lazy<IAbsoluteFilePath> _sourceZip;

            public StellarisMod(IModContent mod, IAbsoluteDirectoryPath contentPath, IAbsoluteDirectoryPath modPath) {
                _sourcePath = contentPath.GetChildDirectoryWithName(mod.PackageName);
                _modPath = modPath;
                _sourceZip =
                    new Lazy<IAbsoluteFilePath>(
                        () => _sourcePath.DirectoryInfo.EnumerateFiles("*.zip").First().ToAbsoluteFilePath());
            }

            public async Task InstallMod() {
                var modName = GetModName();

                var sourceZip = GetSourceZip();
                var installDirectory = _modPath;
                installDirectory.MakeSurePathExists();
                var destinationDir = installDirectory.GetChildDirectoryWithName(modName);
                if (destinationDir.Exists)
                    destinationDir.Delete(true);
                sourceZip.Unpack(destinationDir, true);

                var desc = destinationDir.GetChildFileWithName("descriptor.mod");
                var modFile = installDirectory.GetChildFileWithName($"{modName}.mod");
                modFile.WriteText(
                    desc.ReadAllText()
                        .Replace($"archive=\"{modName}.zip\"", $"path=\"{GetRelModName()}\""));
            }

            public string GetRelModName() => $"mod/{GetModName()}";

            private string GetModName() {
                var sz = GetSourceZip();
                return sz.FileNameWithoutExtension;
            }

            private IAbsoluteFilePath GetSourceZip() => _sourceZip.Value;
        }
    }
}