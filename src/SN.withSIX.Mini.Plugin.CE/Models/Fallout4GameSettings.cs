// <copyright company="SIX Networks GmbH" file="Fallout4GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.CE.Models
{
    [DataContract]
    public class Fallout4GameSettings : GameSettings
    {
        [JsonConstructor]
        public Fallout4GameSettings(Fallout4StartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public Fallout4GameSettings() : this(new Fallout4StartupParameters()) {}
    }
}