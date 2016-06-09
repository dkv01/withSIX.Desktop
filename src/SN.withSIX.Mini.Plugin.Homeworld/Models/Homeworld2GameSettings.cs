// <copyright company="SIX Networks GmbH" file="Homeworld2GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.Homeworld.Models
{
    [DataContract]
    public class Homeworld2GameSettings : GameSettings
    {
        [JsonConstructor]
        public Homeworld2GameSettings(Homeworld2StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public Homeworld2GameSettings() : this(new Homeworld2StartupParameters()) {}
    }
}