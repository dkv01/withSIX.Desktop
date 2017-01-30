// <copyright company="SIX Networks GmbH" file="SteamInfoAttribute.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using withSIX.Api.Models.Games;
using withSIX.Steam.Core;

namespace withSIX.Mini.Core.Games.Attributes
{
    public class SteamInfoAttribute : Attribute
    {
        public static readonly SteamInfoAttribute Default = new NullSteamInfo();
        private readonly string _folderFallback;
        protected SteamInfoAttribute() {}

        public SteamInfoAttribute(uint appId) {
            if (!(appId > 0)) throw new ArgumentOutOfRangeException("appId > 0");

            AppId = appId;
        }

        protected SteamInfoAttribute(SteamGameIds appId) : this((uint) appId) {}

        public SteamInfoAttribute(uint appId, string folderFallback) {
            if (!(appId > 0)) throw new ArgumentOutOfRangeException("appId > 0");
            if (!(!string.IsNullOrWhiteSpace(folderFallback))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(folderFallback)");

            AppId = appId;
            _folderFallback = folderFallback;
        }

        public SteamInfoAttribute(SteamGameIds appId, string folderFallback) : this((uint) appId, folderFallback) {}

        public virtual bool IsValid => true;

        public virtual uint AppId { get; }
        public bool DRM { get; set; }

        public virtual SteamDirectories GetDirectories(ISteamHelper helper) {
            if (!helper.SteamFound)
                return SteamDirectories.Default;

            var si = helper.TryGetSteamAppById(AppId);
            return new SteamDirectories(AppId, si?.GetInstallDir() ?? _folderFallback,
                si?.InstallBase ?? helper.SteamPath);
        }

        class NullSteamInfo : SteamInfoAttribute
        {
            public override uint AppId => 0;
            public override bool IsValid => false;
            public override SteamDirectories GetDirectories(ISteamHelper helper) => SteamDirectories.Default;
        }
    }
}