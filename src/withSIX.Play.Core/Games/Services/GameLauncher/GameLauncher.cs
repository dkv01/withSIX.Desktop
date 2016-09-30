// <copyright company="SIX Networks GmbH" file="GameLauncher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Diagnostics;
using System.Threading.Tasks;
using withSIX.Core;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Logging;
using withSIX.Core.Services;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Core.Games.Services.GameLauncher
{
    public interface IGameLauncher
    {
        Task Notify(IAsyncDomainEvent message);
        Task Notify(ISyncDomainEvent message);
    }

    public interface ILaunch
    {
        Task<Process> Launch(LaunchGameInfo spec);
    }

    public interface ILaunchWithSteamLegacy
    {
        Task<Process> Launch(LaunchGameWithSteamLegacyInfo spec);
    }

    public interface ILaunchWithSteam
    {
        Task<Process> Launch(LaunchGameWithSteamInfo spec);
    }

    public interface IGameLauncherProcess
    {
        Task<Process> LaunchInternal(LaunchGameInfo info);
        Task<Process> LaunchInternal(LaunchGameWithJavaInfo info);
        Task<Process> LaunchInternal(LaunchGameWithSteamInfo info);
        Task<Process> LaunchInternal(LaunchGameWithSteamLegacyInfo info);
    }

    public interface ILaunchWith<in TLaunchHandler> where TLaunchHandler : IGameLauncher {}

    abstract class GameLauncher : IGameLauncher, IEnableLogging, IDomainService
    {
        readonly IGameLauncherProcess _gameLauncherInfra;

        protected GameLauncher(IGameLauncherProcess gameLauncherInfra) {
            _gameLauncherInfra = gameLauncherInfra;
        }

        public Task Notify(IAsyncDomainEvent message) => CalculatedGameSettings.NotifyEnMass(message);
        public Task Notify(ISyncDomainEvent message) => CalculatedGameSettings.NotifyEnMass(message);

        protected Task<Process> LaunchInternal(LaunchGameInfo info) => _gameLauncherInfra.LaunchInternal(info);

        protected Task<Process> LaunchInternal(LaunchGameWithJavaInfo info) => _gameLauncherInfra.LaunchInternal(info);

        protected Task<Process> LaunchInternal(LaunchGameWithSteamInfo info) => _gameLauncherInfra.LaunchInternal(info);

        protected Task<Process> LaunchInternal(LaunchGameWithSteamLegacyInfo info) => _gameLauncherInfra.LaunchInternal(info);
    }

    public class InvalidSteamPathException : UserException
    {
        public InvalidSteamPathException(string message) : base(message) {}
    }
}