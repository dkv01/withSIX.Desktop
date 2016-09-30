// <copyright company="SIX Networks GmbH" file="ShortcutCreator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Applications.Helpers
{
    public class ShortcutCreator
    {
        public static Task CreateDesktopPwsIcon(string name, string description, string arguments,
            IAbsoluteFilePath icon = null) => CreateShortcutAsync(new ShortcutInfo(GetDesktop(), name,
                Common.Paths.EntryLocation) {
                    Arguments = arguments,
                    Icon = icon,
                    Description = description
                });

        public static Task CreateDesktopPwsIconCustomRepo(string name, string description, Uri target,
            IAbsoluteFilePath icon = null) => CreateShortcutAsync(new ShortcutInfo(GetDesktop(), name,
                Common.Paths.EntryLocation) {
                    Arguments = target.ToString(),
                    Description = description,
                    Icon = icon
                });

        public static Task CreateDesktopGameIcon(string name, string description, string arguments, Game game,
            IAbsoluteFilePath icon = null) => CreateShortcutAsync(new ShortcutInfo(GetDesktop(), name,
                game.InstalledState.LaunchExecutable) {
                    WorkingDirectory = game.InstalledState.Directory,
                    Arguments = arguments,
                    Description = description,
                    Icon = icon
                });

        public static Task CreateDesktopGameBat(string name, string description, string arguments, Game game,
            IAbsoluteFilePath icon = null) => Tools.FileUtil.CreateBatFile(GetDesktop(), name,
                GenerateBatContent(game.InstalledState.LaunchExecutable, game.InstalledState.Directory, arguments));

        static string GenerateBatContent(IAbsoluteFilePath target, IAbsoluteDirectoryPath workDir, string pars)
            => String.Format(@"
@echo off
echo Starting: {0}
echo From: {1}
echo With params: {2}
cd /D ""{1}""
""{0}"" {2}", target, workDir, pars);

        static IAbsoluteDirectoryPath GetDesktop()
            => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory).ToAbsoluteDirectoryPath();


        public static Task CreateShortcutAsync(ShortcutInfo info) => Task.Run(() => CreateShortcut(info));

        public static void CreateShortcut(ShortcutInfo info) {
            var sanitizedLinkFile = info.DestinationPath.GetChildFileWithName(MakeValidShortcutFileName(info.Name));
            var shortcut = new ShellLink {
                Target = info.Target.ToString(),
                Arguments = info.Arguments,
                WorkingDirectory = info.WorkingDirectory?.ToString(),
                Description = info.Description,
                IconPath = info.Icon?.ToString()
            };
            shortcut.Save(sanitizedLinkFile.ToString());
        }

        private static string MakeValidShortcutFileName(string fileName) => Tools.FileUtil.MakeValidFileName(fileName, ".lnk");
    }
}