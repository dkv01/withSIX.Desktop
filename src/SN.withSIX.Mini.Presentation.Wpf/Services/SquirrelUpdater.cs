// <copyright company="SIX Networks GmbH" file="SquirrelUpdater.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications;
using Splat;
using Squirrel;

namespace SN.withSIX.Mini.Presentation.Wpf.Services
{
    public class SquirrelApp : ISquirrelApp, IPresentationService
    {
        public async Task<string> GetNewVersion() {
            var updateInfo = await new SquirrelUpdater().CheckForUpdates().ConfigureAwait(false);
            return NotEqualVersions(updateInfo) && HasFutureReleaseEntry(updateInfo)
                ? updateInfo.FutureReleaseEntry.Version.ToString()
                : null;
        }

        static bool HasFutureReleaseEntry(UpdateInfo updateInfo) => updateInfo.FutureReleaseEntry != null;

        static bool NotEqualVersions(UpdateInfo updateInfo)
            => updateInfo.FutureReleaseEntry != updateInfo.CurrentlyInstalledVersion;
    }

    public class SquirrelUpdater : ISquirrelUpdater, IPresentationService
    {
        private static bool _ranVcRedist;

        static SquirrelUpdater() {
            // TODO: Read beta from the executable informationalversion... ?
            var releaseInfo = GetReleaseInfo();
            Info = new SquirrelInfo {
                Uri = new Uri(CommonUrls.SoftwareUpdateUri, "drop/sync" + releaseInfo.Folder),
                Package = "sync" + releaseInfo.Txt
            };
        }

        static SquirrelInfo Info { get; }

        public async Task<UpdateInfo> CheckForUpdates() {
            using (var mgr = GetUpdateManager())
                return await mgr.CheckForUpdate().ConfigureAwait(false);
        }

        public async Task<ReleaseEntry> UpdateApp(Action<int> progressAction) {
            using (var mgr = GetUpdateManager())
                return await mgr.UpdateApp(progressAction).ConfigureAwait(false);
        }

        public void HandleStartup(IReadOnlyCollection<string> arguments) {
            using (var mgr = GetUpdateManager()) {
                // Note, in most of these scenarios, the app exits after this method
                // completes!
                SquirrelAwareApp.HandleEvents(v => InitialInstall(mgr), v => Update(mgr),
                    onAppUninstall: v => {
                        mgr.RemoveShortcutForThisExe();
                        mgr.RemoveUninstallerRegistryEntry();
                    },
                    onFirstRun: () => Consts.FirstRun = true);
            }
            RunVcRedist();
        }

        static void Update(IUpdateManager mgr) {
            mgr.CreateShortcutForThisExe();
            RunVcRedist();
        }

        static void InitialInstall(IUpdateManager mgr) {
            mgr.CreateShortcutForThisExe();
            RunVcRedist();
        }

        static void RunVcRedist(bool force = false) {
            if (_ranVcRedist)
                return;
            _ranVcRedist = true;
            using (
                var p =
                    Process.Start(
                        Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                            "VCRedistInstaller.exe"), "VS2013 VS2012"))
                p.WaitForExit();
        }

        static ReleaseInfo GetReleaseInfo() => new ReleaseInfo(GetTxt());

        static string GetTxt() {
            switch (Common.AppCommon.Type) {
            case ReleaseType.Alpha:
                return "alpha";
            case ReleaseType.Beta:
                return "beta";
            case ReleaseType.Dev:
                return "dev";
            case ReleaseType.Stable:
                return string.Empty;
            default: {
                throw new NotSupportedException("Release type unknown: " + Common.AppCommon.Type);
            }
            }
        }

        public static UpdateManager GetUpdateManager() => new UpdateManager(
            Info.Uri.ToString(),
            Info.Package);

        class ReleaseInfo
        {
            public ReleaseInfo(string txt) {
                Txt = txt;
                Folder = string.IsNullOrEmpty(txt) ? string.Empty : "/" + txt;
                PostFix = string.IsNullOrEmpty(txt) ? string.Empty : "-" + txt;
            }

            public string Txt { get; }
            public string Folder { get; }
            public string PostFix { get; }
        }

        class SquirrelInfo
        {
            public Uri Uri { get; set; }
            public string Package { get; set; }
        }
    }


    class SetupLogLogger : ILogger, IDisposable
    {
        readonly object gate = 42;
        readonly StreamWriter inner;

        public SetupLogLogger(bool saveInTemp) {
            var dir = saveInTemp
                ? Path.GetTempPath()
                : Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var file = Path.Combine(dir, "SquirrelSetup.log");
            if (File.Exists(file))
                File.Delete(file);

            inner = new StreamWriter(file, false, Encoding.UTF8);
        }

        public void Dispose() {
            lock (gate)
                inner.Dispose();
        }

        public LogLevel Level { get; set; }

        public void Write(string message, LogLevel logLevel) {
            if (logLevel < Level)
                return;

            lock (gate)
                inner.WriteLine(message);
        }
    }
}