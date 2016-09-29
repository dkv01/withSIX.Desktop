// <copyright company="SIX Networks GmbH" file="ISteamApp.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using NDepend.Path;

namespace withSIX.Steam.Core
{
    public interface ISteamApp
    {
        uint AppId { get; }
        IAbsoluteDirectoryPath InstallBase { get; }
        IAbsoluteDirectoryPath AppPath { get; }
        bool IsValid { get; }
        string GetInstallDir();
    }
}