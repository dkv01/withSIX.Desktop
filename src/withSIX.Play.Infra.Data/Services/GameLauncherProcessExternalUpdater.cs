// <copyright company="SIX Networks GmbH" file="GameLauncherProcessExternalUpdater.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;

using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Services;
using withSIX.Play.Core.Games.Services.GameLauncher;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Infra.Data.Services
{
    public class GameLauncherProcessExternalUpdater : IEnableLogging, IGameLauncherProcess, IInfrastructureService
    {
        readonly IAbsoluteDirectoryPath _javaPath = null;
        readonly IProcessManager _processManager;

        public GameLauncherProcessExternalUpdater(IProcessManager processManager) {
            _processManager = processManager;
        }

        public Task<Process> LaunchInternal(LaunchGameInfo info) => PerformUpdaterAction(info, new SULaunchDefaultGameArgumentsBuilder(info).Build());

        public Task<Process> LaunchInternal(LaunchGameWithJavaInfo info) => PerformUpdaterAction(info,
            new SULaunchGameJavaArgumentsBuilder(info, _javaPath)
                .Build());

        public Task<Process> LaunchInternal(LaunchGameWithSteamInfo info) => PerformUpdaterAction(info,
            new SULaunchGameSteamArgumentsBuilder(info, GetAndValidateSteamPath(info.SteamDRM, false))
                .Build());

        public Task<Process> LaunchInternal(LaunchGameWithSteamLegacyInfo info) => PerformUpdaterAction(info,
            new SULaunchGameSteamLegacyArgumentsBuilder(info, GetAndValidateSteamPath(info.SteamDRM))
                .Build());

        Task<Process> PerformUpdaterAction(LaunchGameInfoBase spec, IEnumerable<string> args) {
            var startInfo = BuildProcessStartInfo(spec, args);
            return LaunchUpdaterProcess(spec, startInfo);
        }

        static ProcessStartInfo BuildProcessStartInfo(LaunchGameInfoBase spec, IEnumerable<string> args)
            => new ProcessStartInfoBuilder(Common.Paths.ServiceExePath,
                args.CombineParameters()) {
                    WorkingDirectory = spec.WorkingDirectory
                }.Build();

        async Task<Process> LaunchUpdaterProcess(LaunchGameInfoBase spec, ProcessStartInfo startInfo) {
            LogGameInfo(spec);
            LogStartupInfo(startInfo);
            var lInfo = new BasicLaunchInfo(startInfo);
            if (spec.WaitForExit)
                await (spec.LaunchAsAdministrator ? _processManager.LaunchElevatedAsync(lInfo) : _processManager.LaunchAsync(lInfo)).ConfigureAwait(false);
            else
                using (var p = spec.LaunchAsAdministrator ? _processManager.StartElevated(startInfo) : _processManager.Start(startInfo)) { }

            var procName = spec.ExpectedExecutable.FileNameWithoutExtension;
            return await FindGameProcess(procName).ConfigureAwait(false);
        }

        void LogGameInfo(LaunchGameInfoBase spec) {
            // TODO: Par file logging... needs RV specific support..
            this.Logger()
                .Info("Launching the game: {0} from {1}, with: {2}. AsAdmin: {3}. Expecting: {4}", spec.LaunchExecutable,
                    spec.WorkingDirectory, spec.StartupParameters.CombineParameters(), spec.LaunchAsAdministrator,
                    spec.ExpectedExecutable);
        }

        void LogStartupInfo(ProcessStartInfo startInfo) {
            this.Logger().Info("Launching through: " + startInfo.Format());
        }

        static async Task<Process> FindGameProcess(string procName) {
            var startTime = Tools.Generic.GetCurrentUtcDateTime;
            var ts = TimeSpan.FromSeconds(10);
            while (!Tools.Generic.LongerAgoThan(startTime, ts)) {
                var proc = Tools.ProcessManager.Management.FindProcess(procName)
                    .LastOrDefault();
                if (proc != null)
                    return proc;
                await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            }
            return null;
        }

        IAbsoluteDirectoryPath GetAndValidateSteamPath(bool steamDrm, bool requireExeExist = true) => new SteamPathValidator().GetAndValidateSteamPath(steamDrm, requireExeExist);

        class SteamPathValidator
        {
            IAbsoluteDirectoryPath _path;
            bool _requireExeExist;
            bool _steamDrm;
            IAbsoluteFilePath _steamExe;

            public IAbsoluteDirectoryPath GetAndValidateSteamPath(bool steamDrm, bool requireExeExist = true) {
                _steamDrm = steamDrm;
                _requireExeExist = requireExeExist;

                var steamPath = LocalMachineInfo.GetSteamPath();
                if (steamPath == null)
                    throw BuildException();

                _path = steamPath;
                if (!_path.Exists)
                    throw SteamPathDoesntExist();

                ValidateSteamExecutable();

                return _path;
            }

            InvalidSteamPathException SteamPathDoesntExist() => BuildException("Directory does not exist: " + _path);

            void ValidateSteamExecutable() {
                _steamExe = _path.GetChildFileWithName(SteamInfos.SteamExecutable);
                if (_steamExe.Exists)
                    return;

                if (_requireExeExist)
                    throw SteamExeDoesNotExist();

                MainLog.Logger.Warn("Steam executable not found at {0}", _steamExe);
            }

            InvalidSteamPathException SteamExeDoesNotExist() => BuildException("File does not exist: " + _steamExe);

            InvalidSteamPathException BuildException(string error = "") => new InvalidSteamPathException(
        "Steam not detected, please make sure it is installed properly. "
        + DisableSteamOptionsInfo() + "\n" + error);

            string DisableSteamOptionsInfo() => !_steamDrm ? "Or disable 'Launch through Steam' and/or 'Use steam in-game' options" : null;
        }
    }
}