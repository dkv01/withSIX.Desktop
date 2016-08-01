// <copyright company="SIX Networks GmbH" file="StellarisGame.cs">
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

namespace SN.withSIX.Mini.Plugin.Stellaris.Models
{
    [Game(GameUUids.Stellaris, Executables = new[] {@"stellaris.exe"}, Name = "Stellaris", IsPublic = false,
        Slug = "Stellaris")]
    [SteamInfo(281990)]
    [DataContract]
    public class StellarisGame : BasicSteamGame
    {
        public StellarisGame(Guid id, StellarisGameSettings settings) : base(id, settings) {}

        IAbsoluteDirectoryPath GetModInstallationDirectory() => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            .ToAbsoluteDirectoryPath()
            .GetChildDirectoryWithName(@"Paradox Interactive\Stellaris\mod");

        protected override async Task InstallMod(IModContent mod) {
            var sourceDir = ContentPaths.Path.GetChildDirectoryWithName(mod.PackageName);
            var sourceZip = sourceDir.DirectoryInfo.EnumerateFiles("*.zip").First().ToAbsoluteFilePath();
            var modName = sourceZip.FileNameWithoutExtension;

            var installDirectory = GetModInstallationDirectory();
            installDirectory.MakeSurePathExists();
            var destinationDir = installDirectory.GetChildDirectoryWithName(modName);
            if (destinationDir.Exists)
                destinationDir.Delete(true);
            sourceZip.Unpack(destinationDir, true);

            var desc = destinationDir.GetChildFileWithName("descriptor.mod");
            var modFile = installDirectory.GetChildFileWithName($"{modName}.mod");
            modFile.WriteText(
                desc.ReadAllText()
                    .Replace($"archive=\"{modName}.zip\"", $"path=\"mod/{modName}\""));
        }
    }
}