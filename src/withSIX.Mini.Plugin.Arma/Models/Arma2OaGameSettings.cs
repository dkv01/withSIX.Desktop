// <copyright company="SIX Networks GmbH" file="Arma2OaGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace withSIX.Mini.Plugin.Arma.Models
{
    public interface ILaunchAsDedicatedServer
    {
        bool? LaunchAsDedicatedServer { get; set; }
    }

    [DataContract]
    public class Arma2OaGameSettings : Arma2GameSettings, ILaunchAsDedicatedServer
    {
        [JsonConstructor]
        public Arma2OaGameSettings(Arma2OaStartupParameters startupParameters) : base(startupParameters) {
            StartupParameters = startupParameters;
        }

        public Arma2OaGameSettings() : this(new Arma2OaStartupParameters(DefaultStartupParameters)) {}

        [DataMember]
        public bool LaunchThroughBattlEye { get; set; } = true;

        [DataMember]
        [Obsolete("deprecate")]
        public bool? LaunchAsDedicatedServer { get; set; }
    }
}