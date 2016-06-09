// <copyright company="SIX Networks GmbH" file="Arma3GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class Arma3GameSettings : Arma2OaGameSettings
    {
        [JsonConstructor]
        public Arma3GameSettings(Arma3StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public Arma3GameSettings() : this(new Arma3StartupParameters(DefaultStartupParameters)) {}
    }
}