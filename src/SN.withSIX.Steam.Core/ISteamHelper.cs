// <copyright company="SIX Networks GmbH" file="ISteamHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using NDepend.Path;

namespace SN.withSIX.Steam.Core
{
    public interface ISteamHelper
    {
        bool SteamFound { get; }
        IAbsoluteDirectoryPath SteamPath { get; }
        ISteamApp TryGetSteamAppById(uint appId, bool noCache = false);
    }
}