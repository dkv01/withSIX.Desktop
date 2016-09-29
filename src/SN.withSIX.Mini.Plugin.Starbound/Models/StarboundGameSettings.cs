// <copyright company="SIX Networks GmbH" file="StarboundGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Starbound.Models
{
    [DataContract]
    public class StarboundGameSettings : GameSettings
    {
        [JsonConstructor]
        public StarboundGameSettings(StarboundStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public StarboundGameSettings() : this(new StarboundStartupParameters()) {}
    }
}