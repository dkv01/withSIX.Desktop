// <copyright company="SIX Networks GmbH" file="Arma2GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class Arma2GameSettings : RealVirtualityGameSettings
    {
        [JsonConstructor]
        public Arma2GameSettings(Arma2StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public Arma2GameSettings() : this(new Arma2StartupParameters(DefaultStartupParameters)) {}
    }
}