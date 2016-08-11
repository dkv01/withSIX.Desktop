// <copyright company="SIX Networks GmbH" file="SteamInfoAttribute.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
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

        public SteamInfoAttribute(SteamGameIds appId) : this((uint)appId) {}

        [Obsolete("We now only require the appId to get the game folder, use the appId only constructor.")]
        public SteamInfoAttribute(uint appId, string folder) {
            Contract.Requires<ArgumentOutOfRangeException>(appId > 0);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(folder));

            AppId = appId;
            Folder = folder;
        }

        public SteamInfoAttribute(SteamGameIds appId, string folder) : this((uint) appId, folder) {}

        public virtual uint AppId { get; }
        [Obsolete("We now only require the appId to get the game folder")]
        public virtual string Folder { get; }
        public bool DRM { get; set; }

        class NullSteamInfo : SteamInfoAttribute
        {
            public override uint AppId => 0;
        }
    }
}