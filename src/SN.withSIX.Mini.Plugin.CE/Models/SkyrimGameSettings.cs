// <copyright company="SIX Networks GmbH" file="StarboundGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.CE.Models
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