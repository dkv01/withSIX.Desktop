// <copyright company="SIX Networks GmbH" file="IronFrontGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class IronFrontGameSettings : RealVirtualityGameSettings
    {
        [JsonConstructor]
        public IronFrontGameSettings(IronFrontStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public IronFrontGameSettings() : this(new IronFrontStartupParameters(DefaultStartupParameters)) {}
    }
}