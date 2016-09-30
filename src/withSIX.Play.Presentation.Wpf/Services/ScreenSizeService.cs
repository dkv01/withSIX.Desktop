// <copyright company="SIX Networks GmbH" file="ScreenSizeService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Play.Core.Games.Services.GameLauncher;

namespace withSIX.Play.Presentation.Wpf.Services
{
    public class ScreenSizeService : IGetScreenSize, IApplicationService
    {
        public ScreenResolution ScreenSize() => new ScreenResolution(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
    }
}