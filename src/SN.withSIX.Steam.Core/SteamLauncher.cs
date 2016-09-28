// <copyright company="SIX Networks GmbH" file="SteamLauncher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Steam.Core
{
    public class SteamLauncher
    {
        public SteamLauncher(IAbsoluteDirectoryPath steamPath) {
            SteamPath = steamPath;
            if (steamPath != null)
                SteamExePath = steamPath.GetChildFileWithName(SteamInfos.SteamExecutable);
        }

        public IAbsoluteDirectoryPath SteamPath { get; }

        public IAbsoluteFilePath SteamExePath { get; }

        public bool SteamError { get; private set; }

        public bool IsSteamRunning { get; private set; }

        public void DetectSteamRunning() {
            var processes = GetSteamProcesses();
            using (new CompositeDisposable(processes))
                IsSteamRunning = processes.Any();
        }

        void DetectSteamRunningByMainModule() {
            var processes = GetSteamProcesses();
            using (new CompositeDisposable(processes))
                IsSteamRunning = processes.Any(IsSteamMainModule);
        }

        bool IsSteamMainModule(Process p)
            => p.MainModule.FileName.Equals(SteamExePath.ToString(), StringComparison.OrdinalIgnoreCase);

        static Process[] GetSteamProcesses()
            => Tools.ProcessManager.Management.FindProcess(SteamInfos.SteamExecutable);

        void TryDetectSteamRunning() {
            try {
                DetectSteamRunningByMainModule();
            } catch (Exception ex1) {
                MainLog.Logger.FormattedWarnException(ex1, "Steam detection by main module failed");
                try {
                    DetectSteamRunning();
                } catch (Exception ex2) {
                    MainLog.Logger.FormattedWarnException(ex2, "Steam detection failed");
                    SteamError = true;
                }
            }
        }

        public bool StartSteamIfRequired() {
            TryDetectSteamRunning();
            return IsSteamStartRequired() && StartSteam();
        }

        bool IsSteamStartRequired() => !IsSteamRunning && !SteamError;

        bool StartSteam() {
            try {
                TryStartSteam();
                return true;
            } catch (Exception) {
                SteamError = true;
                return false;
            }
        }

        private void TryStartSteam() {
            using (new ProcessStartInfo {
                FileName = SteamExePath.ToString(),
                UseShellExecute = true,
                Arguments = "-forceservice"
            }.Launch()) {}
            WaitForSteamProcess();
        }

        private void WaitForSteamProcess() {
            var tries = 0;
            while (!IsSteamRunning && tries < 10) {
                if (Tools.ProcessManager.Management.FindProcess(SteamInfos.SteamServiceExecutable).Any())
                    IsSteamRunning = true;

                Thread.Sleep(1000);
                tries++;
            }
            if (IsSteamRunning) Thread.Sleep(5000); // additional wait time for Steam getting ready :S
        }

        public bool IsValid() => SteamExePath != null &&
                                 SteamExePath.Exists;

        public static class SteamInfos
        {
            public const string SteamExecutable = "Steam.exe";
            public const string SteamServiceExecutable = "steamservice.exe";
        }
    }

    public class SteamAppLauncher
    {
        private readonly SteamLauncher _steamLauncher;

        public SteamAppLauncher(SteamLauncher steamLauncher) {
            _steamLauncher = steamLauncher;
        }

        public ProcessStartInfo GetLegacyLaunchInfo(uint steamID, string arguments)
            => new ProcessStartInfo {
                FileName = _steamLauncher.SteamExePath.ToString(),
                Arguments = "-applaunch " + steamID + " " + arguments,
                UseShellExecute = true
            };

        public void InjectSteamOverlay(uint steamID, int gamePid) {
            Environment.SetEnvironmentVariable("SteamAppID", Convert.ToString(steamID));
            Environment.SetEnvironmentVariable("SteamGameID", Convert.ToString(steamID));

            //TryInject();
            using (new ProcessStartInfo(_steamLauncher.SteamPath.GetChildFileWithName("GameOverlayUI.exe").ToString(),
                "-pid " + gamePid).Launch())
                ;

            //if (steamID != 0)
            //  TrySteamApiInit();
        }

        /*
        void TrySteamApiInit()
        {
            try
            {
                SteamAPI.Init();
            }
            catch (Exception e)
            {
                MainLog.Logger.FormattedWarnException(e);
                MainLog.Logger.Warn("Abnormal termination - SteamAPI Failed to Initialize");
                _shutdownHandler.Shutdown(1);
            }
        }
        */

        /*
        void TryInject()
        {
            try {
                var injector = new Injector(_launchedGame);
                injector.InjectLibrary(Path.Combine(_steamLauncher.SteamPath, "GameOverlayRenderer.dll"));
            }
            catch (Win32Exception)
            {
                TryInject64(injector);
            }
        }

        void TryInject64()
        {
            try
            {
                injector.InjectLibrary(Path.Combine(_spec.SteamPath,
                    "GameOverlayRenderer64.dll"));
            }
            catch (Win32Exception e)
            {
                MainLog.Logger.FormattedWarnException(e);
            }
        }
        */
    }
}