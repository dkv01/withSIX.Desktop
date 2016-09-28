// <copyright company="SIX Networks GmbH" file="SteamSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Logging;
using SN.withSIX.Steam.Core;
using Steamworks;
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

        private Task Initialize(uint appId, Func<IScheduler, Task> initialize, Action simulate)
            => SetupSteam(appId, initialize, simulate);

        private async Task SetupSteam(uint appId, Func<IScheduler, Task> initialize, Action simulate) {
            Contract.Requires<ArgumentException>(appId > 0);

            await SetupAppId(appId).ConfigureAwait(false);
            StartSteamIfRequiredAndConfirm();
            _scheduler = new EventLoopScheduler();
            await initialize(_scheduler).ConfigureAwait(false);

            _safeCall = new SafeCallFactory().Create();
            _callbackRunner = CreateCallbackRunner(simulate, _cts.Token);
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
                                Trace.WriteLine($"Exception occurred during SteamCallbackRunner: {ex}");
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
                => Do(appId, steamPath, Steamworks.ConfirmSteamInitialization, SteamAPI.RunCallbacks, action);

            public async Task<T> Do<T>(uint appId, IAbsoluteDirectoryPath steamPath,
                Func<IScheduler, Task> initializer, Action simulate, Func<Task<T>> action) {
                using (var session = new SteamSession(steamPath)) {
                    await session.Initialize(appId, initializer, simulate).ConfigureAwait(false);
                    Session = session;
                    return await session.Do(action).ConfigureAwait(false);
                }
            }

            // TODO: Improved approach like DbContextLocator ;-)
            public SteamSession Session { get; private set; }

            async Task<SteamSession> Start(uint appId, IAbsoluteDirectoryPath steamPath,
                Func<IScheduler, Task> initializer, Action simulate) {
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

        Task<T> Do<T>(uint appId, IAbsoluteDirectoryPath steamPath, Func<IScheduler, Task> initializer, Action simulate,
            Func<Task<T>> action);
    }

    public class SafeCallFactory : ISafeCallFactory
    {
        public ISafeCall Create() => new SafeCall();
    }

    public class SafeCall : ISafeCall
    {
        [HandleProcessCorruptedStateExceptions]
        public void Do(Action act) {
            try {
                act();
            } catch (AccessViolationException ex) {
                throw new Exception($"Native exception ocurred while SteamAPI.RunCallbacks(): {ex}");
            } catch (Exception) {
                throw;
            } catch {
                throw new Exception("Unmanged ex");
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public TResult Do<TResult>(Func<TResult> act) {
            try {
                return act();
            } catch (AccessViolationException ex) {
                throw new Exception($"Native exception ocurred while SteamAPI.RunCallbacks(): {ex}");
            } catch (Exception) {
                throw;
            } catch {
                throw new Exception("Unmanged ex");
            }
        }
    }
}