// <copyright company="SIX Networks GmbH" file="GameLauncherProcessInternal.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Play.Core.Games.Services.GameLauncher;

namespace withSIX.Play.Infra.Data.Services
{
    public class GameLauncherProcessInternal : IEnableLogging, IGameLauncherProcess
    {
        public async Task<Process> LaunchInternal(LaunchGameInfo info)
            => Process.Start(new ProcessStartInfo(info.LaunchExecutable.ToString(),
                info.StartupParameters.CombineParameters()));

        public Task<Process> LaunchInternal(LaunchGameWithJavaInfo info) {
            throw new NotImplementedException();
        }

        public Task<Process> LaunchInternal(LaunchGameWithSteamInfo info) {
            throw new NotImplementedException();
        }

        public Task<Process> LaunchInternal(LaunchGameWithSteamLegacyInfo info) {
            throw new NotImplementedException();
        }
    }
}