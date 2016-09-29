// <copyright company="SIX Networks GmbH" file="RealVirtualityGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Arma.Models
{
    public abstract class RealVirtualityGameSettings : GameSettingsWithConfigurablePackageDirectory
    {
        protected static readonly string[] DefaultStartupParameters = {"-nosplash", "-nofilepatching"};

        protected RealVirtualityGameSettings(RealVirtualityStartupParameters startupParameters)
            : base(startupParameters) {}
    }
}