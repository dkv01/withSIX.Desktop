// <copyright company="SIX Networks GmbH" file="Homeworld2GameLauncher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Diagnostics;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Mini.Plugin.Homeworld.Services
{
    public class Homeworld2GameLauncher : GameLauncher, IHomeworld2Launcher
    {
        readonly IGetScreenSize _screenSize;

        public Homeworld2GameLauncher(IMediator mediator, IGameLauncherProcess processManager, IGetScreenSize screenSize)
            : base(mediator, processManager) {
            _screenSize = screenSize;
        }

        public ScreenResolution GetScreenSize() => _screenSize.ScreenSize();

        public Task<Process> Launch(LaunchGameInfo spec) => LaunchInternal(spec);
    }

    public interface IHomeworld2Launcher : IGameLauncher, ILaunch
    {
        ScreenResolution GetScreenSize();
    }
}