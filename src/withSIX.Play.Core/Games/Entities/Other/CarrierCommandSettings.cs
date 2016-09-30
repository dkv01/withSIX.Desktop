// <copyright company="SIX Networks GmbH" file="CarrierCommandSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Core.Games.Entities.RealVirtuality;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Core.Games.Entities.Other
{
    public class CarrierCommandSettings : RealVirtualitySettings
    {
        public CarrierCommandSettings(Guid gameId, CarrierCommandStartupParmeters sp, GameSettingsController controller)
            : base(gameId, sp, controller) {
            StartupParameters = sp;
        }

        public new CarrierCommandStartupParmeters StartupParameters { get; }
    }

    // TODO
    public class CarrierCommandStartupParmeters : ArmaStartupParams
    {
        public CarrierCommandStartupParmeters(params string[] defaultParameters) : base(defaultParameters) {}
    }
}