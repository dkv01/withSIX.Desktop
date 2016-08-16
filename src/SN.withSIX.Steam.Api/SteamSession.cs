// <copyright company="SIX Networks GmbH" file="SteamSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Logging;
using Steamworks;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Steam.Api
{
    public class SteamSession : IDisposable
    {
        private IDisposable _callbackRunner;
        private SteamAPIWarningMessageHook_t _mSteamApiWarningMessageHook;

        private SteamSession(uint appId) {
            AppId = appId;
        }

        public uint AppId { get; }

        public void Dispose() {
            _callbackRunner.Dispose();
            SteamHelper.Scheduler?.Dispose();
            SteamAPI.Shutdown();
        }

        private static void SteamAPIDebugTextHook(int nSeverity, StringBuilder pchDebugText) {
            var message = pchDebugText.ToString();
            MainLog.Logger.Debug(message);
            Console.Error.WriteLine(message);
        }

        public static async Task<SteamSession> Start(uint appId) {
            var session = new SteamSession(appId);
            await session.Initialize(appId).ConfigureAwait(false);
            return session;
        }

        private Task Initialize(uint appId) => SetupSteam(appId);

        private async Task SetupSteam(uint appId) {
            var tmp =
                Directory.GetCurrentDirectory().ToAbsoluteDirectoryPath().GetChildFileWithName("steam_appid.txt");
            await WriteSteamAppId(appId, tmp).ConfigureAwait(false);

            if (!SteamAPI.IsSteamRunning())
                throw new InvalidOperationException("Steam does not appear to be running");

            // TODO: Start Steam
            if (!SteamAPI.Init()) {
                throw new InvalidOperationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            }
            //SteamAPI.RestartAppIfNecessary(new AppId_t(appId));
            _mSteamApiWarningMessageHook = SteamAPIDebugTextHook;
            SteamClient.SetWarningMessageHook(_mSteamApiWarningMessageHook);

            SteamHelper.Scheduler = new EventLoopScheduler();
            _callbackRunner = Observable.Interval(TimeSpan.FromMilliseconds(100), SteamHelper.Scheduler)
                .Do(_ => {
                    try {
                        SteamAPI.RunCallbacks();
                    } catch {
                        Trace.WriteLine("Native exception ocurred while SteamAPI.RunCallbacks()");
                    }
                }).Subscribe();
        }

        private static Task WriteSteamAppId(uint appId, IAbsoluteFilePath steamAppIdFile)
            => appId.ToString().WriteToFileAsync(steamAppIdFile);
    }
}