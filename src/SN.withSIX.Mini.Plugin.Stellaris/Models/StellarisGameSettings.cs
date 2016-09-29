// <copyright company="SIX Networks GmbH" file="StellarisGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Stellaris.Models
{
    [DataContract]
    public class StellarisGameSettings : GameSettings
    {
        [JsonConstructor]
        public StellarisGameSettings(StellarisStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public StellarisGameSettings() : this(new StellarisStartupParameters()) {}
    }
}