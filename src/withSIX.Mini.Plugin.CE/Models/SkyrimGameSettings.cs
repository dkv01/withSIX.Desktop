// <copyright company="SIX Networks GmbH" file="SkyrimGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.CE.Models
{
    [DataContract]
    public class SkyrimGameSettings : GameSettings
    {
        [JsonConstructor]
        public SkyrimGameSettings(SkyrimStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public SkyrimGameSettings() : this(new SkyrimStartupParameters()) {}
    }
}