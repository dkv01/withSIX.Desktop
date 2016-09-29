// <copyright company="SIX Networks GmbH" file="NMSGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.NMS.Models
{
    [DataContract]
    public class NMSGameSettings : GameSettings
    {
        [JsonConstructor]
        public NMSGameSettings(NMSStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public NMSGameSettings() : this(new NMSStartupParameters()) {}
    }
}