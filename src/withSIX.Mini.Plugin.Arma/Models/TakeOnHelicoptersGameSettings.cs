// <copyright company="SIX Networks GmbH" file="TakeOnHelicoptersGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class TakeOnHelicoptersGameSettings : RealVirtualityGameSettings
    {
        [JsonConstructor]
        public TakeOnHelicoptersGameSettings(TakeOnHelicoptersStartupParameters startupParameters)
            : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public TakeOnHelicoptersGameSettings() : this(new TakeOnHelicoptersStartupParameters(DefaultStartupParameters)) {}
    }
}