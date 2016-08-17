// <copyright company="SIX Networks GmbH" file="SteamInfoAttribute.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;
using SN.withSIX.Core;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Core.Games.Attributes
{
    public class SteamInfoAttribute : Attribute
    {
        public static readonly SteamInfoAttribute Default = new NullSteamInfo();
        protected SteamInfoAttribute() {}

        public SteamInfoAttribute(uint appId) {
            Contract.Requires<ArgumentOutOfRangeException>(appId > 0);

            AppId = appId;
        }

        public SteamInfoAttribute(SteamGameIds appId) : this((uint) appId) {}

        public SteamInfoAttribute(uint appId, string folder) {
            Contract.Requires<ArgumentOutOfRangeException>(appId > 0);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(folder));

            AppId = appId;
            Folder = folder;
        }

        public SteamInfoAttribute(SteamGameIds appId, string folder) : this((uint) appId, folder) {}

        public virtual uint AppId { get; }
        // We use this as fallback incase issues querying steam info
        public virtual string Folder { get; }
        public bool DRM { get; set; }

        public SteamDirectories GetDirectories() => new SteamDirectories(this, Common.Paths.SteamPath);

        class NullSteamInfo : SteamInfoAttribute
        {
            public override uint AppId => 0;
        }
    }

    public class SteamDirectories
    {
        public SteamDirectories(SteamInfoAttribute info, IAbsoluteDirectoryPath steamPath) {
            Contract.Requires<ArgumentNullException>(info != null);
            Contract.Requires<ArgumentNullException>(steamPath != null);

            // TODO: Take LibraryPath from KV store
            RootPath = steamPath.GetChildDirectoryWithName("steamapps");
            Game = new SteamGameDirectories(info, RootPath);
            Workshop = new SteamWorkshopDirectories(info, RootPath);
        }

        public IAbsoluteDirectoryPath RootPath { get; }

        public SteamGameDirectories Game { get; }

        public SteamWorkshopDirectories Workshop { get; }
    }

    public class SteamWorkshopDirectories
    {
        public SteamWorkshopDirectories(SteamInfoAttribute info, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath.GetChildDirectoryWithName("workshop");
            ContentPath = RootPath.GetChildDirectoryWithName("content").GetChildDirectoryWithName(info.AppId.ToString());
        }

        public IAbsoluteDirectoryPath RootPath { get; }
        public IAbsoluteDirectoryPath ContentPath { get; }
    }

    public class SteamGameDirectories
    {
        public SteamGameDirectories(SteamInfoAttribute info, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath.GetChildDirectoryWithName("common");
            ContentPath = RootPath.GetChildDirectoryWithName(info.Folder); // TODO: Take FolderName from KV store.
        }

        public IAbsoluteDirectoryPath RootPath { get; }
        public IAbsoluteDirectoryPath ContentPath { get; }
    }
}