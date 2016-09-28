// <copyright company="SIX Networks GmbH" file="SteamPathHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;

namespace SN.withSIX.Steam.Core
{
    public class SteamPathHelper
    {
        private static readonly string steamRegistry = @"SOFTWARE\Valve\Steam";
        private static IAbsoluteDirectoryPath _steamPath;
        public static IAbsoluteDirectoryPath SteamPath => _steamPath ?? (_steamPath = GetSteamPathInternal());

        private static IAbsoluteDirectoryPath GetSteamPathInternal() {
            var regPath = TryGetPathFromRegistry();
            if (regPath != null && regPath.Exists)
                return regPath;
            var expectedPath =
                PathConfiguration.GetFolderPath(EnvironmentSpecial.SpecialFolder.ProgramFilesX86)
                    .ToAbsoluteDirectoryPath()
                    .GetChildDirectoryWithName("Steam");
            return expectedPath.Exists ? expectedPath : null;
        }

        private static IAbsoluteDirectoryPath TryGetPathFromRegistry() {
            var p = Tools.Generic.NullSafeGetRegKeyValue<string>(steamRegistry, "InstallPath");
            return p.IsBlankOrWhiteSpace() ? null : p.Trim().ToAbsoluteDirectoryPath();
        }
    }
}