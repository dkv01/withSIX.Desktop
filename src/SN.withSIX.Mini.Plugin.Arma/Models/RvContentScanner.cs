// <copyright company="SIX Networks GmbH" file="RvContentScanner.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Mini.Core.Games;
using withSIX.Sync.Core.Packages;

namespace withSIX.Mini.Plugin.Arma.Models
{
    // TODO: Include online network content?
    public class RvContentScanner
    {
        readonly RealVirtualityGame _realVirtualityGame;

        public RvContentScanner(RealVirtualityGame realVirtualityGame) {
            _realVirtualityGame = realVirtualityGame;
        }

        public IEnumerable<LocalContent> ScanForNewContent(IReadOnlyCollection<string> dlcs,
                IEnumerable<IAbsoluteDirectoryPath> paths)
            => paths.SelectMany(x => GetMods(x, dlcs));

        IEnumerable<LocalContent> GetMods(IAbsoluteDirectoryPath d, IEnumerable<string> dlcs)
            => d.ChildrenDirectoriesPath
                .Where(x => !dlcs.ContainsIgnoreCase(x.DirectoryName) && !x.IsEmptySafe())
                .Select(HandleContent)
                .Where(x => x != null);

        LocalContent HandleContent(IAbsoluteDirectoryPath path) {
            var networkContents = _realVirtualityGame.NetworkContent.OfType<ModNetworkContent>();
            var packageName = path.DirectoryName;
            var nc = networkContents.FirstOrDefault(
                x => x.PackageName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase));
            // ?? networkContents.FirstOrDefault(x => x.Aliases.ContainsIgnoreCase(packageName))

            var version = GetVersion(path);
            // TODO: Hidden state change!
            // TODO: Steam vs withSIX check!
            if ((nc != null) && !nc.IsSteam()) {
                if ((nc.InstallInfo == null) || (nc.InstallInfo.Version != version) ||
                    ((version != null) && !nc.InstallInfo.Completed))
                    nc.Installed(version, true);
            }
            var existingLocalContent = FindLocalContent(packageName);
            if ((nc != null) && (existingLocalContent != null))
                _realVirtualityGame.Contents.Remove(existingLocalContent);

            return nc == null ? ScanForAddonFolders(path) : null;
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

        LocalContent ScanForAddonFolders(IAbsoluteDirectoryPath path) {
            var dirs = new[] {"addons", "dta", "common", "dll"};
            if (dirs.Any(x => !path.GetChildDirectoryWithName(x).IsEmptySafe())) {
                return !HasContentAlready(path.DirectoryName)
                    ? new ModLocalContent(path.DirectoryName.ToLower(), _realVirtualityGame.Id,
                        new BasicInstallInfo())
                    : null;
            }
            return null;
        }
    }
}