// <copyright company="SIX Networks GmbH" file="SteamSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using Steamworks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Logging;
using withSIX.Core.Services;
using withSIX.Steam.Core;

namespace withSIX.Steam.Api.Services
{
    public interface ISteamSession
    {
        IScheduler Scheduler { get; }
        uint AppId { get; }
    }

    public class SteamSession : IDisposable, ISteamSession
    {
        private readonly CancellationTokenSource _cts;
        private readonly IAbsoluteDirectoryPath _steamPath;
        private Task _callbackRunner;
        private ISafeCall _safeCall;
        private EventLoopScheduler _scheduler;

        private SteamSession(IAbsoluteDirectoryPath steamPath) {
            _steamPath = steamPath;
            _cts = new CancellationTokenSource();
        }

        public void Dispose() {
            _cts.Dispose();
            _scheduler?.Dispose();
        }

        public IScheduler Scheduler => _scheduler;

        public uint AppId { get; private set; }

        public async Task Shutdown() {
            _cts.Cancel();
            try {
                await _callbackRunner;
            } catch (OperationCanceledException) {} finally {
                SteamAPI.Shutdown();
            }
        }

        private Task<T> Initialize<T>(uint appId, Func<IScheduler, Task<T>> initialize, Action simulate)
            => SetupSteam(appId, initialize, simulate);

        private async Task<T> SetupSteam<T>(uint appId, Func<IScheduler, Task<T>> initialize, Action simulate) {
            Contract.Requires<ArgumentException>(appId > 0);

            _safeCall = LockedWrapper.callFactory.Create();
            await SetupAppId(appId).ConfigureAwait(false);
            StartSteamIfRequiredAndConfirm();
            _scheduler = new EventLoopScheduler();
            var init = await initialize(_scheduler).ConfigureAwait(false);

            _callbackRunner = CreateCallbackRunner(simulate, _cts.Token);
            return init;
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
            if ((_steamPath == null) || !_steamPath.Exists)
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


        private async Task CreateCallbackRunner(Action act, CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                await Observable.Return(Unit.Default, _scheduler)
                    .Do(x => {
                        if (!ct.IsCancellationRequested)
                            try {
                                _safeCall.Do(act);
                            } catch (Exception ex) {
                                //Trace.WriteLine($"Exception occurred during SteamCallbackRunner: {ex}");
                                Console.Error.WriteLine($"Exception occurred during SteamCallbackRunner: {ex}");
                                throw;
                            }
                    });
                await Task.Delay(TimeSpan.FromMilliseconds(50), ct).ConfigureAwait(false);
            }
        }

        public async Task<T> Do<T>(Func<Task<T>> action) {
            try {
                return await action().ConfigureAwait(false);
            } finally {
                await Shutdown().ConfigureAwait(false);
            }
        }

        public class SteamSessionFactory : ISteamSessionFactory, ISteamSessionLocator
        {
            public Task<T> Do<T>(uint appId, IAbsoluteDirectoryPath steamPath, Func<Task<T>> action)
                => Do(appId, steamPath, async s => {
                    await Steamworks.ConfirmSteamInitialization(s).ConfigureAwait(false);
                    return (IDisposable) null;
                }, SteamAPI.RunCallbacks, _ => action());

            public async Task<T> Do<T, TInit>(uint appId, IAbsoluteDirectoryPath steamPath,
                Func<IScheduler, Task<TInit>> initializer, Action simulate, Func<TInit, Task<T>> action) where TInit : IDisposable {
                using (var session = new SteamSession(steamPath)) {
                    using (var api = await session.Initialize(appId, initializer, simulate).ConfigureAwait(false)) {
                        Session = session;
                        return await session.Do(() => action(api)).ConfigureAwait(false);
                    }
                }
            }

            // TODO: Improved approach like DbContextLocator ;-)
            public SteamSession Session { get; private set; }

            async Task<SteamSession> Start<T>(uint appId, IAbsoluteDirectoryPath steamPath,
                Func<IScheduler, Task<T>> initializer, Action simulate) {
                var session = new SteamSession(steamPath);
                await session.Initialize(appId, initializer, simulate).ConfigureAwait(false);
                Session = session;
                return session;
            }

            static class Steamworks
            {
                private static SteamAPIWarningMessageHook_t _mSteamApiWarningMessageHook;

                internal static async Task ConfirmSteamInitialization(IScheduler scheduler) {
                    if (!SteamAPI.Init()) {
                        throw new SteamInitializationException(
                            "Steam initialization failed. Is Steam running under the same priviledges?");
                    }
                    SetupDebugHook();
                }

                private static void SetupDebugHook() {
                    _mSteamApiWarningMessageHook = SteamAPIDebugTextHook;
                    SteamClient.SetWarningMessageHook(_mSteamApiWarningMessageHook);
                }

                private static void SteamAPIDebugTextHook(int nSeverity, StringBuilder pchDebugText) {
                    var message = pchDebugText.ToString();
                    MainLog.Logger.Debug(message);
                    Console.Error.WriteLine(message);
                }
            }
        }
    }

    public interface ISteamSessionLocator
    {
        SteamSession Session { get; }
    }

    public interface ISteamSessionFactory
    {
        Task<T> Do<T>(uint appId, IAbsoluteDirectoryPath steamPath, Func<Task<T>> action);

        Task<T> Do<T, TInit>(uint appId, IAbsoluteDirectoryPath steamPath, Func<IScheduler, Task<TInit>> initializer, Action simulate,
            Func<TInit, Task<T>> action) where TInit : IDisposable;
    }
}