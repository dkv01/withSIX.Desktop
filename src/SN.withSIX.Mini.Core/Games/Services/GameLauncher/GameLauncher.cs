// <copyright company="SIX Networks GmbH" file="GameLauncher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services;

namespace SN.withSIX.Mini.Core.Games.Services.GameLauncher
{
    public interface IGameLauncher
    {
        Task Notify<T>(T message);
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

    public abstract class GameLauncher : IGameLauncher, IEnableLogging, IDomainService
    {
        readonly IGameLauncherProcess _gameLauncherInfra;
        readonly IMediator _mediator;

        protected GameLauncher(IMediator mediator, IGameLauncherProcess gameLauncherInfra) {
            _mediator = mediator;
            _gameLauncherInfra = gameLauncherInfra;
        }

        public Task Notify<T>(T message) => _mediator.NotifyEnMass(message);

        protected Task<Process> LaunchInternal(LaunchGameInfo info) => _gameLauncherInfra.LaunchInternal(info);

        protected Task<Process> LaunchInternal(LaunchGameWithJavaInfo info) => _gameLauncherInfra.LaunchInternal(info);

        protected Task<Process> LaunchInternal(LaunchGameWithSteamInfo info) => _gameLauncherInfra.LaunchInternal(info);

        protected Task<Process> LaunchInternal(LaunchGameWithSteamLegacyInfo info)
            => _gameLauncherInfra.LaunchInternal(info);
    }

    public class InvalidSteamPathException : Exception
    {
        public InvalidSteamPathException(string message) : base(message) {}
    }
}