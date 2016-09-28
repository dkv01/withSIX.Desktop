// <copyright company="SIX Networks GmbH" file="GameLauncherProcessInternal.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;
using SN.withSIX.Steam.Core;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    // TODO: Decide if we should relaunch the withSIX process, more like the withSIX-Updater, or stick to current implementation.
    // (outputting the LaunchGameInfos to console commands, and picking them up again and transforming them to LaunchSpec)
    // That kind of processing would then need to happen before the App.xaml.cs's 'SingleInstance' processing has been completed.
    public class GameLauncherProcessInternal : IEnableLogging, IGameLauncherProcess
    {
        private readonly IRestarter _restarter;

        public GameLauncherProcessInternal(IRestarter restarter) {
            _restarter = restarter;
        }

        public async Task<Process> LaunchInternal(LaunchGameInfo info)
            => Process.GetProcessById(CreateLauncher().LaunchGame(info.ToLaunchSpec()));

        public async Task<Process> LaunchInternal(LaunchGameWithJavaInfo info)
            => Process.GetProcessById(CreateLauncher().LaunchGame(info.ToLaunchSpec()));

        public async Task<Process> LaunchInternal(LaunchGameWithSteamInfo info)
            => Process.GetProcessById(CreateLauncher().LaunchGame(info.ToLaunchSpec()));

        public async Task<Process> LaunchInternal(LaunchGameWithSteamLegacyInfo info)
            => Process.GetProcessById(CreateLauncher().LaunchGame(info.ToLaunchSpec()));

        private GameLauncher CreateLauncher() => new GameLauncher(_restarter);

        // TODO: Support for the SteamAPI/Injector
        // TODO: Re-use the external launcher option - by relaunching sync.exe with the commandline params etc.
        public class GameLauncher
        {
            readonly IRestarter _restarter;
            //readonly IShutdownHandler _shutdownHandler;
            ProcessStartInfo _gameStartInfo;
            bool _isSteamGameAndAvailable;
            bool _isSteamValid;
            Process _launchedGame;
            GameLaunchSpec _spec;
            private SteamAppLauncher _steamAppLauncher;
            private SteamLauncher _steamLauncher;

            public GameLauncher(IRestarter restarter) {
                _restarter = restarter;
            }

            public int LaunchGame(GameLaunchSpec spec) {
                Contract.Requires<ArgumentNullException>(spec != null);
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(spec.GamePath));
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(spec.WorkingDirectory));

                _spec = spec;
                _steamLauncher = new SteamLauncher(_spec.SteamPath);
                _steamAppLauncher = new SteamAppLauncher(_steamLauncher);

                PrepareSteamState();
                if (_spec.LegacyLaunch && _isSteamGameAndAvailable)
                    LegacySteamLaunch();
                else
                    ModernLaunch();

                using (_launchedGame) {
                    if (_launchedGame == null)
                        return -1;
                    PostProcessLaunch();
                    return _launchedGame.Id;
                }
            }

            void PostProcessLaunch() {
                SetPriority();
                if (_spec.Affinity != null && _spec.Affinity.Any())
                    SetAffinity();
            }

            void SetAffinity() {
                try {
                    Tools.ProcessManager.Management.SetAffinity(_launchedGame, _spec.Affinity);
                } catch (Win32Exception ex) {
                    if (ex.NativeErrorCode != Win32ErrorCodes.ACCESS_DENIED)
                        throw;
                    MainLog.Logger.FormattedWarnException(ex, "Access denied while trying to set process affinity");
                } catch (InvalidOperationException ex) {
                    MainLog.Logger.FormattedWarnException(ex, "Error while trying to set process affinity");
                }
            }

            void SetPriority() {
                try {
                    _launchedGame.PriorityClass = _spec.Priority;
                } catch (Win32Exception ex) {
                    if (ex.NativeErrorCode != Win32ErrorCodes.ACCESS_DENIED)
                        throw;
                    MainLog.Logger.FormattedWarnException(ex, "Access denied while trying to set process priority");
                } catch (InvalidOperationException ex) {
                    MainLog.Logger.FormattedWarnException(ex, "Error while trying to set process priority");
                }
            }

            void PrepareSteamState() => _isSteamGameAndAvailable = _spec.SteamID != 0 && _steamLauncher.IsValid();

            void LegacySteamLaunch() {
                PrepareLegacySteamLaunch();
                StartGame();
            }

            void PrepareLegacySteamLaunch()
                => _gameStartInfo = _steamAppLauncher.GetLegacyLaunchInfo(_spec.SteamID, _spec.Arguments);

            void ModernLaunch() {
                StartSteamIfRequired();
                ValidateSteamRunning();
                PrepareModernGameLaunch();
                StartGame();
                //InjectSteamOverlayIfNeeded();
            }

            void PrepareModernGameLaunch() {
                _gameStartInfo = new ProcessStartInfo {
                    FileName = _spec.GamePath,
                    Arguments = _spec.Arguments,
                    UseShellExecute = true
                };

                if (!string.IsNullOrWhiteSpace(_spec.WorkingDirectory))
                    _gameStartInfo.WorkingDirectory = _spec.WorkingDirectory;

                if (!_isSteamValid)
                    return;
                _gameStartInfo.UseShellExecute = false; // This breaks UAC prompts if the game is set to require UAC, in that case we should already be running as admin ourselves...
                var steamId = Convert.ToString(_spec.SteamID);
                Tools.ProcessManager.Management.AddEnvironmentVariables(_gameStartInfo, new Dictionary<string, string> {
                    {
                        "SteamAppID", steamId
                    }, {
                        "SteamGameID", steamId
                    }
                });
            }

            void StartSteamIfRequired() {
                if (!_isSteamGameAndAvailable)
                    return;
                _steamLauncher.StartSteamIfRequired();
            }

            void ValidateSteamRunning() {
                MainLog.Logger.Info("Steam Info: {0}, {1}, {2}", _isSteamGameAndAvailable, _steamLauncher.IsSteamRunning,
                    _steamLauncher.SteamError);
                _isSteamValid = _isSteamGameAndAvailable && _steamLauncher.IsSteamRunning && !_steamLauncher.SteamError;
            }

            void StartGame() {
                if (_spec.BypassUAC)
                    LaunchGameWithUacBypass();
                else
                    LaunchGameWithoutUacBypass();

                if (_launchedGame == null)
                    return;
                TrySetForeground(_launchedGame);
            }

            void LaunchGameWithUacBypass()
                => _launchedGame = Process.GetProcessById(LaunchWithUacBypass(_gameStartInfo));

            int LaunchWithUacBypass(ProcessStartInfo gameStartInfo) {
                MainLog.Logger.Info("LaunchingAsUser {0} from {1} with {2}", gameStartInfo.FileName,
                    gameStartInfo.WorkingDirectory, gameStartInfo.Arguments);
                return ServiceStartProcess.StartProcessAndBypassUAC(gameStartInfo);
            }

            void LaunchGameWithoutUacBypass() {
                try {
                    _launchedGame = _gameStartInfo.Launch();
                } catch (Win32Exception ex) {
                    if (ex.NativeErrorCode == 740) {
                        if (!Tools.UacHelper.CheckUac())
                            throw;
                        _restarter.RestartWithUacInclEnvironmentCommandLine();
                        _launchedGame = null;
                    }
                    throw;
                }
            }

            static void TrySetForeground(Process launchedGame) {
                try {
                    throw new NotImplementedException();
                    //Tools.ProcessManager.ManagementTools.NativeMethods.SetForeground(launchedGame);
                } catch (Exception e) {
                    MainLog.Logger.FormattedWarnException(e);
                }
            }

            void InjectSteamOverlayIfNeeded() {
                if (_spec.SteamDRM || !_isSteamValid)
                    return;
                _steamAppLauncher.InjectSteamOverlay(_spec.SteamID, _launchedGame.Id);
            }

            public class GameLaunchSpec
            {
                public string Arguments { get; set; }
                public bool BypassUAC { get; set; }
                public string GamePath { get; set; }
                public bool LegacyLaunch { get; set; }
                public bool SteamDRM { get; set; }
                public uint SteamID { get; set; }
                public IAbsoluteDirectoryPath SteamPath { get; set; }
                public string WorkingDirectory { get; set; }
                public ProcessPriorityClass Priority { get; set; }
                public int[] Affinity { get; set; }
            }

            public static class ServiceStartProcess
            {
                public static int StartProcessAndBypassUAC(ProcessStartInfo startInfo) {
                    uint winlogonPid = 0;
                    IntPtr hUserTokenDup = IntPtr.Zero, hPToken = IntPtr.Zero, hProcess = IntPtr.Zero;
                    var procInfo = new PROCESS_INFORMATION();

                    // obtain the currently active session id; every logged on user in the system has a unique session id
                    var dwSessionId = WTSGetActiveConsoleSessionId();

                    // obtain the process id of the winlogon process that is running within the currently active session
                    var processes = Process.GetProcessesByName("winlogon");
                    foreach (var p in processes) {
                        if ((uint) p.SessionId == dwSessionId)
                            winlogonPid = (uint) p.Id;
                    }

                    // obtain a handle to the winlogon process
                    hProcess = OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);

                    // obtain a handle to the access token of the winlogon process
                    if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hPToken)) {
                        CloseHandle(hProcess);
                        return 0;
                    }

                    // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser
                    // I would prefer to not have to use a security attribute variable and to just 
                    // simply pass null and inherit (by default) the security attributes
                    // of the existing token. However, in C# structures are value types and therefore
                    // cannot be assigned the null value.
                    var sa = new SECURITY_ATTRIBUTES();
                    sa.Length = Marshal.SizeOf(sa);

                    // copy the access token of the winlogon process; the newly created token will be a primary token
                    if (
                        !DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa,
                            (int) SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int) TOKEN_TYPE.TokenPrimary,
                            ref hUserTokenDup)) {
                        CloseHandle(hProcess);
                        CloseHandle(hPToken);
                        return 0;
                    }

                    // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning
                    // the window station has a desktop that is invisible and the process is incapable of receiving
                    // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
                    // interaction with the new process.
                    var si = new STARTUPINFO();
                    si.cb = Marshal.SizeOf(si);
                    si.lpDesktop = @"winsta0\default";
                    // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop

                    // flags that specify the priority and creation method of the process
                    const int dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;

                    // create a new process in the current user's logon session
                    var result = CreateProcessAsUser(hUserTokenDup, // client's access token
                        startInfo.FileName, // file to execute
                        startInfo.Arguments, // command line
                        ref sa, // pointer to process SECURITY_ATTRIBUTES
                        ref sa, // pointer to thread SECURITY_ATTRIBUTES
                        false, // handles are not inheritable
                        dwCreationFlags, // creation flags
                        IntPtr.Zero, // pointer to new environment block 
                        startInfo.WorkingDirectory, // name of current directory 
                        ref si, // pointer to STARTUPINFO structure
                        out procInfo // receives information about new process
                        );

                    // invalidate the handles
                    CloseHandle(hProcess);
                    CloseHandle(hPToken);
                    CloseHandle(hUserTokenDup);

                    return (int) (result ? procInfo.dwProcessId : 0); // return the result
                }

                #region Structures

                [StructLayout(LayoutKind.Sequential)]
                public struct PROCESS_INFORMATION
                {
                    public IntPtr hProcess;
                    public IntPtr hThread;
                    public uint dwProcessId;
                    public uint dwThreadId;
                }

                [StructLayout(LayoutKind.Sequential)]
                public struct SECURITY_ATTRIBUTES
                {
                    public int Length;
                    public IntPtr lpSecurityDescriptor;
                    public bool bInheritHandle;
                }

                [StructLayout(LayoutKind.Sequential)]
                public struct STARTUPINFO
                {
                    public int cb;
                    public string lpReserved;
                    public string lpDesktop;
                    public string lpTitle;
                    public uint dwX;
                    public uint dwY;
                    public uint dwXSize;
                    public uint dwYSize;
                    public uint dwXCountChars;
                    public uint dwYCountChars;
                    public uint dwFillAttribute;
                    public uint dwFlags;
                    public short wShowWindow;
                    public short cbReserved2;
                    public IntPtr lpReserved2;
                    public IntPtr hStdInput;
                    public IntPtr hStdOutput;
                    public IntPtr hStdError;
                }

                #endregion

                #region Enumerations

                enum SECURITY_IMPERSONATION_LEVEL
                {
                    SecurityAnonymous = 0,
                    SecurityIdentification = 1,
                    SecurityImpersonation = 2,
                    SecurityDelegation = 3
                }

                enum TOKEN_TYPE
                {
                    TokenPrimary = 1,
                    TokenImpersonation = 2
                }

                #endregion

                #region Constants

                public const int TOKEN_DUPLICATE = 0x0002;
                public const uint MAXIMUM_ALLOWED = 0x2000000;
                public const int CREATE_NEW_CONSOLE = 0x00000010;

                public const int IDLE_PRIORITY_CLASS = 0x40;
                public const int NORMAL_PRIORITY_CLASS = 0x20;
                public const int HIGH_PRIORITY_CLASS = 0x80;
                public const int REALTIME_PRIORITY_CLASS = 0x100;

                #endregion

                #region Win32 API Imports

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern bool CloseHandle(IntPtr hSnapshot);

                [DllImport("kernel32.dll")]
                static extern uint WTSGetActiveConsoleSessionId();

                [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true,
                    CharSet = CharSet.Ansi,
                    CallingConvention = CallingConvention.StdCall)]
                public static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName,
                    string lpCommandLine,
                    ref SECURITY_ATTRIBUTES lpProcessAttributes,
                    ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags,
                    IntPtr lpEnvironment,
                    string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
                    out PROCESS_INFORMATION lpProcessInformation);

                [DllImport("kernel32.dll")]
                static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

                [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
                public static extern bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
                    ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType,
                    int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

                [DllImport("kernel32.dll")]
                static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

                [DllImport("advapi32", SetLastError = true) /*, SuppressUnmanagedCodeSecurity */]
                static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

                #endregion
            }
        }
    }

    public static class LaunchGameInfoExtensions
    {
        public static GameLauncherProcessInternal.GameLauncher.GameLaunchSpec ToLaunchSpec(this LaunchGameInfoBase info)
            => new GameLauncherProcessInternal.GameLauncher.GameLaunchSpec {
                GamePath = info.LaunchExecutable.ToString(),
                WorkingDirectory = info.WorkingDirectory.ToString(),
                SteamPath = SteamPathHelper.SteamPath,
                Arguments = info.StartupParameters.CombineParameters(),
                Priority = info.Priority,
                Affinity = info.Affinity
            };

        public static GameLauncherProcessInternal.GameLauncher.GameLaunchSpec ToLaunchSpec(
            this LaunchGameWithSteamInfo info) {
            var spec = ((LaunchGameInfoBase) info).ToLaunchSpec();
            spec.SteamDRM = info.SteamDRM;
            spec.SteamID = info.SteamAppId;

            return spec;
        }

        public static GameLauncherProcessInternal.GameLauncher.GameLaunchSpec ToLaunchSpec(
            this LaunchGameWithSteamLegacyInfo info) {
            var spec = ((LaunchGameWithSteamInfo) info).ToLaunchSpec();
            spec.LegacyLaunch = true;

            return spec;
        }
    }
}