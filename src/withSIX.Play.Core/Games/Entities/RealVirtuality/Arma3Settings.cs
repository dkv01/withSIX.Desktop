// <copyright company="SIX Networks GmbH" file="Arma3Settings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using withSIX.Play.Core.Games.Legacy.Arma;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Core.Games.Entities.RealVirtuality
{
    public class Arma3Settings : Arma2OaSettings
    {
        public Arma3Settings(Guid gameId, Arma3StartupParameters startupParameters, GameSettingsController controller)
            : base(gameId, startupParameters, controller) {
            StartupParameters = startupParameters;
        }

        public new Arma3StartupParameters StartupParameters { get; }
    }

    public class Arma3StartupParameters : ArmaStartupParams
    {
        public Arma3StartupParameters(params string[] defaultParameters) : base(defaultParameters) {}
        [Category(GameSettingCategories.Advanced), Description(
            "Disable logging to improve performance"
            )]
        public bool NoLogs
        {
            get { return GetSwitchOrDefault(); }
            set { SetSwitchOrDefault(value); }
        }
        [Category(GameSettingCategories.Advanced), Description(
            @"Enables the use of hyper-threading CPU cores which might slightly improve performance in certain scenarios. Note that this option may be overriden by -cpuCount so if you want to use maximum number of CPU cores use -enableHT without -cpuCount."
            )]
        public bool EnableHt
        {
            get { return GetSwitchOrDefault(); }
            set { SetSwitchOrDefault(value); }
        }
    }
}