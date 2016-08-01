// <copyright company="SIX Networks GmbH" file="RvContentScanner.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDepend.Path;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Sync.Core.Packages;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    public class RvContentScanner
    {
        readonly RealVirtualityGame _realVirtualityGame;

        public RvContentScanner(RealVirtualityGame realVirtualityGame) {
            _realVirtualityGame = realVirtualityGame;
        }

        public IEnumerable<LocalContent> ScanForNewContent(IReadOnlyCollection<string> dlcs,
            IEnumerable<IAbsoluteDirectoryPath> paths)
            => paths.SelectMany(x => GetMods(x, dlcs));

        IEnumerable<LocalContent> GetMods(IAbsoluteDirectoryPath d, IReadOnlyCollection<string> dlcs)
            =>
                d.DirectoryInfo.GetDirectories()
                    .Where(x => !dlcs.ContainsIgnoreCase(x.Name))
                    .Select(HandleContent)
                    .Where(x => x != null);

        LocalContent HandleContent(FileSystemInfo dir) {
            var networkContents = _realVirtualityGame.NetworkContent.OfType<ModNetworkContent>();
            var packageName = dir.Name;
            var nc = networkContents.FirstOrDefault(
                x => x.PackageName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase));
            // ?? networkContents.FirstOrDefault(x => x.Aliases.ContainsIgnoreCase(packageName))

            var path = dir.FullName.ToAbsoluteDirectoryPath();
            var version = GetVersion(path);
            // TODO: Hidden state change!
            // TODO: Steam vs withSIX check!
            if (nc != null && !nc.IsSteam()) {
                if (nc.InstallInfo == null || nc.InstallInfo.Version != version ||
                    (version != null && !nc.InstallInfo.Completed))
                    nc.Installed(version, true);
            }
            var existingLocalContent = FindLocalContent(packageName);
            if (nc != null && existingLocalContent != null)
                _realVirtualityGame.Contents.Remove(existingLocalContent);

            return nc == null ? ScanForAddonFolders(dir) : null;
        }

        private string GetVersion(IAbsoluteDirectoryPath path) {
            var v = Package.ReadSynqInfoFile(path);
            return v?.VersionData;
        }

        bool HasContentAlready(string value) => FindInstalledContent(value) != null;

        private IPackagedContent FindInstalledContent(string value)
            => _realVirtualityGame.InstalledContent.OfType<IPackagedContent>().FirstOrDefault(
                x => x.PackageName.Equals(value, StringComparison.CurrentCultureIgnoreCase));

        private LocalContent FindLocalContent(string value) => _realVirtualityGame.LocalContent.FirstOrDefault(
            x => x.PackageName.Equals(value, StringComparison.CurrentCultureIgnoreCase));

        LocalContent ScanForAddonFolders(FileSystemInfo dir) {
            var di = dir.FullName.ToAbsoluteDirectoryPath();
            var dirs = new[] {"addons", "dta", "common", "dll"};
            if (dirs.Any(x => di.GetChildDirectoryWithName(x).Exists)) {
                return !HasContentAlready(dir.Name)
                    ? new ModLocalContent(dir.Name, dir.Name.ToLower(), _realVirtualityGame.Id, new BasicInstallInfo())
                    : null;
            }
            return null;
        }
    }
}