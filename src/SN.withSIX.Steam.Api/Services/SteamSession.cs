// <copyright company="SIX Networks GmbH" file="SteamSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Logging;
using Steamworks;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Steam.Api.Services
{
    public interface ISteamSession
    {
        IScheduler Scheduler { get; }
        uint AppId { get; }
    }

    public class SteamSession : IDisposable, ISteamSession
    {
        private IDisposable _callbackRunner;
        private SteamAPIWarningMessageHook_t _mSteamApiWarningMessageHook;
        private EventLoopScheduler _scheduler;
        private IAbsoluteDirectoryPath _steamPath;

        private SteamSession(IAbsoluteDirectoryPath steamPath) {
            _steamPath = steamPath;
        }

        public void Dispose() {
            _callbackRunner.Dispose();
            _scheduler?.Dispose();
            SteamAPI.Shutdown();
        }

        public IScheduler Scheduler => _scheduler;

        public uint AppId { get; private set; }

        private static void SteamAPIDebugTextHook(int nSeverity, StringBuilder pchDebugText) {
            var message = pchDebugText.ToString();
            MainLog.Logger.Debug(message);
            Console.Error.WriteLine(message);
        }

        private Task Initialize(uint appId) => SetupSteam(appId);

        private async Task SetupSteam(uint appId) {
            Contract.Requires<ArgumentException>(appId > 0);

            await SetupAppId(appId).ConfigureAwait(false);
            StartSteamIfRequiredAndConfirm();
            ConfirmSteamInitialization();
            SetupDebugHook();

            _scheduler = new EventLoopScheduler();
            _callbackRunner = CreateCallbackRunner();
        }

        private async Task SetupAppId(uint appId) {
            if (AppId > 0)
                throw new InvalidOperationException("This session is already initialized!");
            AppId = appId;
            var tmp =
                Directory.GetCurrentDirectory().ToAbsoluteDirectoryPath().GetChildFileWithName("steam_appid.txt");
            await WriteSteamAppId(appId, tmp).ConfigureAwait(false);
        }

        private static Task WriteSteamAppId(uint appId, IAbsoluteFilePath steamAppIdFile)
            => appId.ToString().WriteToFileAsync(steamAppIdFile);

        private void StartSteamIfRequiredAndConfirm() {
            if (SteamAPI.IsSteamRunning())
                return;
            ConfirmSteamPath();
            StartSteam();
            ConfirmSteamRunning();
        }

        private void ConfirmSteamPath() {
            if (_steamPath == null || !_steamPath.Exists)
                throw new SteamNotFoundException("Steam does not appear to be running and could not find Steam");
        }

        private void StartSteam() {
            var sl = new SteamLauncher(_steamPath);
            if (!sl.SteamExePath.Exists)
                throw new SteamNotFoundException("Steam does not appear to be running and could not find Steam");
            sl.StartSteamIfRequired();
        }

        private static void ConfirmSteamRunning() {
            if (!SteamAPI.IsSteamRunning())
                throw new SteamInitializationException("Steam does not appear to be running");
        }

        private static void ConfirmSteamInitialization() {
            if (!SteamAPI.Init()) {
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            }
        }

        private void SetupDebugHook() {
            _mSteamApiWarningMessageHook = SteamAPIDebugTextHook;
            SteamClient.SetWarningMessageHook(_mSteamApiWarningMessageHook);
        }

        private IDisposable CreateCallbackRunner() =>
            Observable.Interval(TimeSpan.FromMilliseconds(100), Scheduler)
                .Do(_ => {
                    try {
                        SteamAPI.RunCallbacks();
                    } catch (Exception) {
                        throw;
                    } catch {
                        Trace.WriteLine("Native exception ocurred while SteamAPI.RunCallbacks()");
                        Console.Error.WriteLine("Native exception ocurred while SteamAPI.RunCallbacks()");
                    }
                }).Subscribe();

        public class SteamSessionFactory : ISteamSessionFactory, ISteamSessionLocator
        {
            public async Task<SteamSession> Start(uint appId, IAbsoluteDirectoryPath steamPath) {
                var session = new SteamSession(steamPath);
                await session.Initialize(appId).ConfigureAwait(false);
                Session = session;
                return session;
            }

            // TODO: Improved approach like DbContextLocator ;-)
            public SteamSession Session { get; private set; }
        }
    }

    public interface ISteamSessionLocator
    {
        SteamSession Session { get; }
    }

    public interface ISteamSessionFactory
    {
        Task<SteamSession> Start(uint appId, IAbsoluteDirectoryPath steamPath);
    }

    public class DidNotStartException : UserException
    {
        public DidNotStartException(string message) : base(message) { }
        public DidNotStartException(string message, Exception ex) : base(message, ex) { }
    }

    public class SteamInitializationException : DidNotStartException
    {
        public SteamInitializationException(string message) : base(message) {}
    }

    public class SteamNotFoundException : DidNotStartException
    {
        public SteamNotFoundException(string message) : base(message) {}
    }
}