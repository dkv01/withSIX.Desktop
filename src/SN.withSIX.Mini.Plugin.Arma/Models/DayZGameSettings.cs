// <copyright company="SIX Networks GmbH" file="DayZGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class DayZGameSettings : RealVirtualityGameSettings
    {
        [JsonConstructor]
        public DayZGameSettings(DayZStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public DayZGameSettings() : this(new DayZStartupParameters(DefaultStartupParameters)) {}
    }
}