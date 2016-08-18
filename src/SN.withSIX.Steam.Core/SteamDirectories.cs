// <copyright company="SIX Networks GmbH" file="SteamDirectories.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace SN.withSIX.Steam.Core
{
    public class SteamDirectories
    {
        public SteamDirectories(uint appId, string folder, IAbsoluteDirectoryPath steamPath) {
            Contract.Requires<ArgumentNullException>(appId > 0);
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(steamPath != null);

            // TODO: Take LibraryPath from KV store
            RootPath = steamPath.GetChildDirectoryWithName("steamapps");
            Game = new SteamGameDirectories(folder, RootPath);
            Workshop = new SteamWorkshopDirectories(appId, RootPath);
        }

        public IAbsoluteDirectoryPath RootPath { get; }

        public SteamGameDirectories Game { get; }

        public SteamWorkshopDirectories Workshop { get; }
    }

    public class SteamWorkshopDirectories
    {
        public SteamWorkshopDirectories(uint appId, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath.GetChildDirectoryWithName("workshop");
            ContentPath = RootPath.GetChildDirectoryWithName("content").GetChildDirectoryWithName(appId.ToString());
        }

        public IAbsoluteDirectoryPath RootPath { get; }
        public IAbsoluteDirectoryPath ContentPath { get; }
    }

    public class SteamGameDirectories
    {
        public SteamGameDirectories(string folder, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath.GetChildDirectoryWithName("common");
            ContentPath = RootPath.GetChildDirectoryWithName(folder);
        }

        public IAbsoluteDirectoryPath RootPath { get; }
        public IAbsoluteDirectoryPath ContentPath { get; }
    }
}