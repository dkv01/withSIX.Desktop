﻿// <copyright company="SIX Networks GmbH" file="StellarisGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Plugin.Stellaris.Models
{
    [Game(GameUUids.Stellaris, Executables = new[] {@"stellaris.exe"}, Name = "Stellaris", IsPublic = true,
        Slug = "Stellaris")]
    [SynqRemoteInfo(GameUUids.Stellaris)]
    [SteamInfo(SteamGameIds.Stellaris)]
    [DataContract]
    public class StellarisGame : BasicSteamGame
    {
        public StellarisGame(Guid id, StellarisGameSettings settings) : base(id, settings) {}

        IAbsoluteDirectoryPath GetModInstallationDirectory() => GetDocumentsDirectory().GetChildDirectoryWithName("mod");

        private static IAbsoluteDirectoryPath GetDocumentsDirectory()
            => Common.Paths.MyDocumentsPath.GetChildDirectoryWithName(@"Paradox Interactive\Stellaris");

        protected override async Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            var sf = GetDocumentsDirectory().GetChildFileWithName("settings.txt");
            new SettingsWriter(sf).Write(GetMods(launchContentAction));
        }

        private IEnumerable<StellarisMod> GetMods(ILaunchContentAction<IContent> launchContentAction)
            => launchContentAction.Content.Select(x => x.Content).OfType<IModContent>().Select(CreateMod);

        protected override Task InstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Install();
        }

        protected override Task UninstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Uninstall();
        }

        private StellarisMod CreateMod(IModContent x)
            => new StellarisMod(GetContentSourceDirectory(x), GetModInstallationDirectory());

        class SettingsWriter
        {
            private const string ModStart = "last_mods={";
            private const string ModEnd = "}";
            private IEnumerable<StellarisMod> _mods;
            private readonly IAbsoluteFilePath _sf;
            private StringBuilder _settings;

            public SettingsWriter(IAbsoluteFilePath sf) {
                _sf = sf;
            }

            internal void Write(IEnumerable<StellarisMod> mods) {
                _mods = mods;

                ReadSettingsFile();
                WriteNewModsSection();
                WriteSettingsFile();
            }

            private void ReadSettingsFile() => _settings = GetSettingsWithoutMods();

            private StringBuilder GetSettingsWithoutMods() {
                var settings = new StringBuilder();
                var settingsLines = _sf.ReadLines();
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

            private void WriteNewModsSection() {
                _settings.AppendLine(ModStart);
                foreach (var m in _mods)
                    _settings.AppendLine($"\t\"{m.GetRelModName()}.mod\"");
                _settings.AppendLine(ModEnd);
            }

            private void WriteSettingsFile() => _sf.WriteText(_settings.ToString());
        }

        class StellarisMod
        {
            private readonly IAbsoluteDirectoryPath _modPath;
            private readonly IAbsoluteDirectoryPath _sourcePath;
            private readonly Lazy<IAbsoluteFilePath> _sourceZip;

            public StellarisMod(IAbsoluteDirectoryPath contentPath, IAbsoluteDirectoryPath modPath) {
                _sourcePath = contentPath;
                _modPath = modPath;
                _sourceZip =
                    new Lazy<IAbsoluteFilePath>(
                        () => _sourcePath.DirectoryInfo.EnumerateFiles("*.zip").First().ToAbsoluteFilePath());
            }

            public async Task Install() {
                var modName = GetModName();
                var sourceZip = GetSourceZip();
                _modPath.MakeSurePathExists();
                var destinationDir = _modPath.GetChildDirectoryWithName(modName);
                if (destinationDir.Exists)
                    destinationDir.Delete(true);
                sourceZip.Unpack(destinationDir, true);

                var desc = destinationDir.GetChildFileWithName("descriptor.mod");
                var modFile = _modPath.GetChildFileWithName($"{modName}.mod");
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

            public async Task Uninstall() {
                if (!_modPath.Exists)
                    return;
                var modName = GetModName();
                var destinationDir = _modPath.GetChildDirectoryWithName(modName);
                if (destinationDir.Exists)
                    destinationDir.Delete(true);
                else {
                    var modFile = _modPath.GetChildFileWithName($"{modName}.mod");
                    if (modFile.Exists)
                        modFile.Delete();
                }
            }
        }
    }
}