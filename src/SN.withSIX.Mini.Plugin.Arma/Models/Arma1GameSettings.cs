// <copyright company="SIX Networks GmbH" file="Arma1GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class Arma1GameSettings : RealVirtualityGameSettings
    {
        [JsonConstructor]
        public Arma1GameSettings(Arma1StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public Arma1GameSettings() : this(new Arma1StartupParameters(DefaultStartupParameters)) {}
    }
}