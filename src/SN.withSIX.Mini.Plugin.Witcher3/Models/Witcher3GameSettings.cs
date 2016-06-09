// <copyright company="SIX Networks GmbH" file="Witcher3GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.Witcher3.Models
{
    [DataContract]
    public class Witcher3GameSettings : GameSettings
    {
        [JsonConstructor]
        public Witcher3GameSettings(Witcher3StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public Witcher3GameSettings() : this(new Witcher3StartupParameters()) {}
    }
}