// <copyright company="SIX Networks GmbH" file="TakeOnMarsSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Core.Games.Entities.RealVirtuality;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Core.Games.Entities.Other
{
    public class TakeOnMarsSettings : RealVirtualitySettings
    {
        public TakeOnMarsSettings(Guid gameId, TakeOnMarsStartupParams sp, GameSettingsController controller)
            : base(gameId, sp, controller) {
            StartupParameters = sp;
        }

        public new TakeOnMarsStartupParams StartupParameters { get; }
    }

    // TODO
    public class TakeOnMarsStartupParams : RealVirtualityStartupParameters
    {
        public TakeOnMarsStartupParams(params string[] parameters) : base(parameters) {}
    }
}