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
using SN.withSIX.Core.Logging;
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
        private IDisposable _callbackRunner;
        private SteamAPIWarningMessageHook_t _mSteamApiWarningMessageHook;
        private EventLoopScheduler _scheduler;


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
            if (AppId > 0)
                throw new InvalidOperationException("This session is already initialized!");
            AppId = appId;
            var tmp =
                Directory.GetCurrentDirectory().ToAbsoluteDirectoryPath().GetChildFileWithName("steam_appid.txt");
            await WriteSteamAppId(appId, tmp).ConfigureAwait(false);

            if (!SteamAPI.IsSteamRunning())
                throw new SteamInitializationException("Steam does not appear to be running");

            // TODO: Start Steam
            if (!SteamAPI.Init()) {
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            }
            //SteamAPI.RestartAppIfNecessary(new AppId_t(appId));
            _mSteamApiWarningMessageHook = SteamAPIDebugTextHook;
            SteamClient.SetWarningMessageHook(_mSteamApiWarningMessageHook);

            _scheduler = new EventLoopScheduler();
            _callbackRunner = Observable.Interval(TimeSpan.FromMilliseconds(100), Scheduler)
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
        }

        private static Task WriteSteamAppId(uint appId, IAbsoluteFilePath steamAppIdFile)
            => appId.ToString().WriteToFileAsync(steamAppIdFile);

        public class SteamSessionFactory : ISteamSessionFactory, ISteamSessionLocator
        {
            // TODO: Improved approach like DbContextLocator ;-)
            public SteamSession Session { get; private set; }

            public async Task<SteamSession> Start(uint appId) {
                var session = new SteamSession();
                await session.Initialize(appId).ConfigureAwait(false);
                Session = session;
                return session;
            }
        }
    }

    public interface ISteamSessionLocator
    {
        SteamSession Session { get; }
    }

    public interface ISteamSessionFactory
    {
        Task<SteamSession> Start(uint appId);
    }

    public class SteamInitializationException : InvalidOperationException
    {
        public SteamInitializationException(string message) : base(message) {}
    }
}