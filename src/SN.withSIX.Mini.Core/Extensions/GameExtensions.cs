// <copyright company="SIX Networks GmbH" file="GameExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Steam.Core;
using withSIX.Api.Models.Content;

namespace SN.withSIX.Mini.Core.Extensions
{
    public static class GameExtensions
    {
        // TODO: Build via Attributes?
        private static readonly Publisher[] externals = {
            Publisher.NoMansSkyMods, Publisher.NexusMods, Publisher.Chucklefish,
            Publisher.ModDb, Publisher.Curse
        };

        public static bool ShouldInstallFromExternal(this Publisher p) => externals.Contains(p);

        public static IAbsoluteDirectoryPath TryGetDefaultDirectory(this RegistryInfoAttribute registryInfo) {
            if (registryInfo.Path != null) {
                var path = Tools.Generic.NullSafeGetRegKeyValue<string>(registryInfo.Path, registryInfo.Key);
                if (path != null && path.IsValidAbsoluteDirectoryPath())
                    return path.ToAbsoluteDirectoryPath();
            }
            return null;
        }

        public static IAbsoluteDirectoryPath TryGetDefaultDirectory(this SteamInfoAttribute steamInfo) {
            var steamApp = TryGetSteamApp(steamInfo);
            return steamApp.IsValid ? steamApp.AppPath : null;
        }

        public static SteamApp TryGetSteamApp(this SteamInfoAttribute steamInfo) {
            if (steamInfo.AppId <= 0 || !Game.SteamHelper.SteamFound)
                return SteamApp.Default;
            return Game.SteamHelper.TryGetSteamAppById(steamInfo.AppId);
        }

        public static bool HasPublisher(this IEnumerable<ContentPublisher> This, Publisher publisher) => This.Any(p => p.Publisher == publisher);

        public static ContentPublisher GetPublisher(this IEnumerable<ContentPublisher> This, Publisher publisher) => This.Single(x => x.Publisher == publisher);
    }
}