// <copyright company="SIX Networks GmbH" file="KerbalSPGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.Kerbal.Models
{
    [DataContract]
    public class KerbalSPGameSettings : GameSettings
    {
        [JsonConstructor]
        public KerbalSPGameSettings(KerbalSPStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public KerbalSPGameSettings() : this(new KerbalSPStartupParameters()) {}
    }
}