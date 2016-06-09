// <copyright company="SIX Networks GmbH" file="CarrierCommandGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    // TODO
    [DataContract]
    public class CarrierCommandGameSettings : GameSettingsWithConfigurablePackageDirectory
    {
        [JsonConstructor]
        public CarrierCommandGameSettings(CarrierCommandStartupParmeters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public CarrierCommandGameSettings() : this(new CarrierCommandStartupParmeters()) {}
    }
}