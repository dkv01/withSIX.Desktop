// <copyright company="SIX Networks GmbH" file="GTA5GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.GTA.Models
{
    [DataContract]
    public class GTA5GameSettings : GameSettings
    {
        [JsonConstructor]
        public GTA5GameSettings(GTA5StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public GTA5GameSettings() : this(new GTA5StartupParameters()) {}
    }
}