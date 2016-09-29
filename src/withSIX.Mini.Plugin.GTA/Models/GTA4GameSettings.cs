// <copyright company="SIX Networks GmbH" file="GTA4GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.GTA.Models
{
    [DataContract]
    public class GTA4GameSettings : GameSettings
    {
        [JsonConstructor]
        public GTA4GameSettings(GTA4StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public GTA4GameSettings() : this(new GTA4StartupParameters()) {}
    }
}