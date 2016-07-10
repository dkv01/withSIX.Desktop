// <copyright company="SIX Networks GmbH" file="BasicGameLauncher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Diagnostics;
using System.Threading.Tasks;

namespace SN.withSIX.Mini.Core.Games.Services.GameLauncher
{
    public interface IBasicGameLauncher : IGameLauncher, ILaunch, ILaunchWithSteam {}

    class BasicGameLauncher : GameLauncher, IBasicGameLauncher
    {
        public BasicGameLauncher(IGameLauncherProcess processManager)
            : base(processManager) {}

        public Task<Process> Launch(LaunchGameInfo spec) => LaunchInternal(spec);

        public Task<Process> Launch(LaunchGameWithSteamInfo spec) => LaunchInternal(spec);
    }
}