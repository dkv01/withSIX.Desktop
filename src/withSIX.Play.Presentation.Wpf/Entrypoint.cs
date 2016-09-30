// <copyright company="SIX Networks GmbH" file="Entrypoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using withSIX.Core;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Wpf;
using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Play.Applications.Services;
using Squirrel;
using UpdateManager = Squirrel.UpdateManager;

namespace withSIX.Play.Presentation.Wpf
{
    public class Entrypoint
    {
        private static bool _ranVcRedist;
        const string AppName = "Play withSIX";

        [STAThread]
        public static void Main() {
            try {
                Initialize();
            } catch (Exception ex) {
                TryLogException(ex);
                throw;
            }
        }

        static void Initialize() {
            CommonBase.AssemblyLoader = new AssemblyLoader(Assembly.GetEntryAssembly());
            HandleSquirrel();

            if (
                !SingleInstance<PlayApp>.TryInitializeAsFirstInstance<PlayApp>(AppName, null, "withSIX-Play",
                    "Play withSIX", "Play"))
                return;

            StartupSequence.PreInit(AppName);
            PlayApp.Launch();

            SingleInstance<PlayApp>.Cleanup();
        }

        static void HandleSquirrel() {
            /*
            var isUninstalling = System.Environment.GetCommandLineArgs().Any(x => x.Contains("uninstall"));
            using (var logger = new SetupLogLogger(isUninstalling) { Level = Splat.LogLevel.Info }) {
                Splat.Locator.CurrentMutable.Register(() => logger, typeof (Splat.ILogger));
*/
            using (var mgr = SquirrelUpdater.GetUpdateManager()) {
                // Note, in most of these scenarios, the app exits after this method
                // completes!
                SquirrelAwareApp.HandleEvents(v => InitialInstall(mgr), v => Update(mgr),
                    onAppUninstall: v => {
                        mgr.RemoveShortcutForThisExe();
                        if (MessageBox.Show(
                            "Do you wish to keep the Configuration data, WARNING: Choosing 'No' cannot be undone!",
                            "Keep the configuration data?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                            UninstallConfigurationData();
                        mgr.RemoveUninstallerRegistryEntry();
                    } //,
                    //onFirstRun: () => ShowTheWelcomeWizard = true
                    );
            }
            RunVcRedist();
        }

        static void Update(UpdateManager mgr) {
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
                            "VCRedistInstaller.exe"), "VS2015 VS2013 VS2012"))
                p.WaitForExit();
        }

        static void UninstallConfigurationData() {
            foreach (
                var dir in
                    new[]
                    {Common.Paths.DataPath, Common.Paths.LocalDataPath, Common.Paths.LocalDataRootPath}
                        .Where(x => x.Exists).Select(x => x.DirectoryInfo))
                dir.Delete(true);
        }

        static void TryLogException(Exception ex) {
            try {
                MainLog.Logger.FormattedErrorException(ex, "Abnormal termination");
            } catch {}
        }
    }
}