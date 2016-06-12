// <copyright company="SIX Networks GmbH" file="GameLauncherProcessExternal.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public class GameLauncherProcessExternalUpdater : IEnableLogging, IGameLauncherProcess, IInfrastructureService
    {
        readonly IPathConfiguration _pathConfiguration;
        readonly IProcessManager _processManager;

        public GameLauncherProcessExternalUpdater(IProcessManager processManager, IPathConfiguration pathConfiguration) {
            _processManager = processManager;
            _pathConfiguration = pathConfiguration;
        }

        public Task<Process> LaunchInternal(LaunchGameInfo info)
            => PerformUpdaterAction(info, new SULaunchDefaultGameArgumentsBuilder(info).Build());

        public Task<Process> LaunchInternal(LaunchGameWithJavaInfo info) => PerformUpdaterAction(info,
            new SULaunchGameJavaArgumentsBuilder(info, _pathConfiguration.JavaPath)
                .Build());

        public Task<Process> LaunchInternal(LaunchGameWithSteamInfo info) => PerformUpdaterAction(info,
            new SULaunchGameSteamArgumentsBuilder(info, GetAndValidateSteamPath(info.SteamDRM, false))
                .Build());

        //[ReportUsage("Legacy Steam Launch")]
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
                    AsAdministrator = spec.LaunchAsAdministrator,
                    WorkingDirectory = spec.WorkingDirectory
                }.Build();

        async Task<Process> LaunchUpdaterProcess(LaunchGameInfoBase spec, ProcessStartInfo startInfo) {
            LogGameInfo(spec);
            LogStartupInfo(startInfo);
            if (spec.WaitForExit) {
                if (spec.LaunchAsAdministrator)
                    await LaunchAsAdmin(startInfo).ConfigureAwait(false);
                else {
                    var lResult = await LaunchNormally(startInfo).ConfigureAwait(false);
                    try {
                        var p = Process.GetProcessById(lResult.ProcessId);
                        var path = Tools.Processes.GetProcessPath(lResult.ProcessId);
                        if (path != null && path.Equals(spec.ExpectedExecutable))
                            return p;
                    } catch (ArgumentException) {}
                }
            } else
                using (var p = _processManager.Start(startInfo)) {}
            var procName = spec.ExpectedExecutable.FileNameWithoutExtension;
            return await FindGameProcess(procName).ConfigureAwait(false);
        }

        private async Task<LaunchResult> LaunchNormally(ProcessStartInfo startInfo) {
            var info =
                await _processManager.LaunchAndGrabAsync(new BasicLaunchInfo(startInfo)).ConfigureAwait(false);
            if (info.ExitCode != 0)
                throw new Exception("Launching failed with error code " + info.ExitCode);
            var lResult =
                Tools.Serialization.Json.LoadJson<LaunchResult>(
                    info.StandardOutput.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                        .Last());
            return lResult;
        }

        private async Task LaunchAsAdmin(ProcessStartInfo startInfo) {
            var info = await _processManager.LaunchAsync(new BasicLaunchInfo(startInfo)).ConfigureAwait(false);
            if (info.ExitCode != 0)
                throw new Exception("Launching failed with error code " + info.ExitCode);
        }

        public class LaunchResult
        {
            public int ProcessId { get; set; }
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
            var ts = TimeSpan.FromSeconds(20);
            while (!Tools.Generic.LongerAgoThan(startTime, ts)) {
                var proc = Tools.Processes.FindProcess(procName)
                    .LastOrDefault();
                if (proc != null)
                    return proc;
                await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            }
            return null;
        }

        IAbsoluteDirectoryPath GetAndValidateSteamPath(bool steamDrm, bool requireExeExist = true)
            => new SteamPathValidator().GetAndValidateSteamPath(steamDrm, requireExeExist);

        class SteamPathValidator
        {
            IAbsoluteDirectoryPath _path;
            bool _requireExeExist;
            bool _steamDrm;
            IAbsoluteFilePath _steamExe;

            public IAbsoluteDirectoryPath GetAndValidateSteamPath(bool steamDrm, bool requireExeExist = true) {
                _steamDrm = steamDrm;
                _requireExeExist = requireExeExist;

                var steamPath = Common.Paths.SteamPath;
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
                _steamExe = _path.GetChildFileWithName(GameLauncherProcessInternal.SteamInfos.SteamExecutable);
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

            string DisableSteamOptionsInfo()
                => !_steamDrm ? "Or disable 'Launch through Steam' and/or 'Use steam in-game' options" : null;
        }
    }

    abstract class SULaunchGameArgumentsBuilder
    {
        protected readonly LaunchGameInfoBase Spec;

        protected SULaunchGameArgumentsBuilder(LaunchGameInfoBase spec) {
            Contract.Requires<ArgumentNullException>(spec != null);
            Contract.Requires<ArgumentNullException>(spec.LaunchExecutable != null);
            Contract.Requires<ArgumentNullException>(spec.WorkingDirectory != null);
            Contract.Requires<ArgumentNullException>(spec.StartupParameters != null);
            Spec = spec;
        }

        protected string GetStartupParameters() => "--arguments=" + Spec.StartupParameters.CombineParameters();

        public virtual IEnumerable<string> Build() => new[] {
            UpdaterCommands.LaunchGame,
            "--gamePath=" + Spec.LaunchExecutable,
            "--workingDirectory=" + Spec.WorkingDirectory,
            GetPriority(),
            GetAffinity(),
            //GetRealExe(),
            GetStartupParameters()
        }.Where(x => x != null);

        protected string GetAffinity() => Spec.Affinity != null && Spec.Affinity.Any()
            ? "--affinity=" + string.Join(",", Spec.Affinity)
            : null;

        protected string GetPriority() => "--priority=" + Spec.Priority;

        protected string GetRealExe() {
            if (Spec.LaunchExecutable != Spec.ExpectedExecutable)
                return "--realExe=" + Spec.ExpectedExecutable;
            return null;
        }
    }

    class SULaunchDefaultGameArgumentsBuilder : SULaunchGameArgumentsBuilder
    {
        public SULaunchDefaultGameArgumentsBuilder(LaunchGameInfo spec) : base(spec) {}

        public override IEnumerable<string> Build() => new[] {
            UpdaterCommands.LaunchGame,
            "--gamePath=" + Spec.LaunchExecutable,
            "--workingDirectory=" + Spec.WorkingDirectory,
            GetPriority(),
            GetAffinity(),
            //GetRealExe(),
            GetStartupParameters()
        }.Where(x => x != null);
    }

    class SULaunchGameJavaArgumentsBuilder : SULaunchGameArgumentsBuilder
    {
        readonly IAbsoluteDirectoryPath _javaPath;

        public SULaunchGameJavaArgumentsBuilder(LaunchGameWithJavaInfo spec, IAbsoluteDirectoryPath javaPath)
            : base(spec) {
            Contract.Requires<ArgumentNullException>(javaPath != null);
            _javaPath = javaPath;
        }

        public override IEnumerable<string> Build() {
            throw new NotImplementedException();
        }
    }

    class SULaunchGameSteamArgumentsBuilder : SULaunchGameArgumentsBuilder
    {
        readonly LaunchGameWithSteamInfo _spec;
        readonly IAbsoluteDirectoryPath _steamPath;

        public SULaunchGameSteamArgumentsBuilder(LaunchGameWithSteamInfo spec, IAbsoluteDirectoryPath steamPath)
            : base(spec) {
            Contract.Requires<ArgumentNullException>(steamPath != null);
            Contract.Requires<ArgumentNullException>(spec.SteamAppId != -1);
            _steamPath = steamPath;
            _spec = spec;
        }

        protected string GetSteamAppId() => "--steamID=" + _spec.SteamAppId;

        protected string GetSteamPath() => "--steamPath=" + _steamPath;

        public override IEnumerable<string> Build() => new[] {
            UpdaterCommands.LaunchGame,
            "--gamePath=" + Spec.LaunchExecutable,
            "--workingDirectory=" + Spec.WorkingDirectory,
            GetSteamDRM(),
            GetSteamAppId(),
            GetSteamPath(),
            GetPriority(),
            GetAffinity(),
            //GetRealExe(),
            GetStartupParameters()
        }.Where(x => x != null);

        string GetSteamDRM() => _spec.SteamDRM ? "--steamDRM" : null;
    }

    class SULaunchGameSteamLegacyArgumentsBuilder : SULaunchGameSteamArgumentsBuilder
    {
        public SULaunchGameSteamLegacyArgumentsBuilder(LaunchGameWithSteamLegacyInfo spec,
            IAbsoluteDirectoryPath steamPath)
            : base(spec, steamPath) {}

        public override IEnumerable<string> Build() => new[] {
            UpdaterCommands.LaunchGame,
            "--gamePath=" + Spec.LaunchExecutable,
            "--workingDirectory=" + Spec.WorkingDirectory,
            "--legacyLaunch",
            GetSteamAppId(),
            GetSteamPath(),
            GetPriority(),
            GetAffinity(),
            //GetRealExe(),
            GetStartupParameters()
        }.Where(x => x != null);
    }
}