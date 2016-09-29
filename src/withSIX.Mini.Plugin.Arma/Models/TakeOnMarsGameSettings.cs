// <copyright company="SIX Networks GmbH" file="TakeOnMarsGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public class TakeOnMarsGameSettings : GameSettingsWithConfigurablePackageDirectory
    {
        [JsonConstructor]
        public TakeOnMarsGameSettings(TakeOnMarsStartupParams startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public TakeOnMarsGameSettings() : this(new TakeOnMarsStartupParams()) {}
    }
}