// <copyright company="SIX Networks GmbH" file="ScreenResolution.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core;

namespace withSIX.Play.Core.Games.Services.GameLauncher
{
    public interface IGetScreenSize
    {
        ScreenResolution ScreenSize();
    }
}