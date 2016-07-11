// <copyright company="SIX Networks GmbH" file="Entrypoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SimpleInjector;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Assemblies;
using SN.withSIX.Core.Presentation.Logging;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Core.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Presentation.Core;
using Splat;
using ILogger = Splat.ILogger;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public static class Entrypoint
    {
        private static ElectronAppBootstrapper _bootstrapper;
        private static string[] _args;
        //static AppBootstrapper _bs;

        public static INodeApi Api { get; private set; }

        public static void MainForNode(INodeApi api) {
            Api = api;
            Main();
        }

        private static async void LaunchAppThread() {
            try {
                await Task.Factory.StartNew(async () => {
                    // todo; ex handling
                    _bootstrapper = new ElectronAppBootstrapper(new Container(), Locator.CurrentMutable, _args);
                    await StartupInternal().ConfigureAwait(false);
                }, TaskCreationOptions.LongRunning).Unwrap().ConfigureAwait(false);
            } catch (Exception ex) {
                Cheat.SetErrorred(ex);
                throw;
            }

            /*
            var newWindowThread = new Thread(() => {
                // Start the Dispatcher Processing
                //System.Windows.Threading.Dispatcher.Run();
                Api.Exit(StartApp()).WaitAndUnwrapException();
            });
            // Set the apartment state
            newWindowThread.SetApartmentState(ApartmentState.STA);
            // Make the thread a background thread
            newWindowThread.IsBackground = true;
            // Start the thread
            newWindowThread.Start();

                 */
        }

        private static Task StartupInternal() => _bootstrapper.Startup(() => TaskExt.Default);

        private static void SetupVersion() {
            Consts.InternalVersion = CommonBase.AssemblyLoader.GetEntryVersion().ToString();
            Consts.ProductVersion = CommonBase.AssemblyLoader.GetInformationalVersion();
        }

        public static void ExitForNode() => _bootstrapper.Dispose();

        static void SetupAssemblyLoader(IAbsoluteFilePath locationOverride = null) {
            CommonBase.AssemblyLoader = new AssemblyLoader(typeof (Entrypoint).Assembly, locationOverride);
        }

        static void SetupRegistry() {
            var registry = new AssemblyRegistry();
            AppDomain.CurrentDomain.AssemblyResolve += registry.CurrentDomain_AssemblyResolve;
        }

        static void LaunchWithNode() {
            // TODO: Node version
            //new RuntimeCheckWpf().Check();

            SetupRegistry();
            var exe = Environment.GetCommandLineArgs().First();
            SetupAssemblyLoader(exe.IsValidAbsoluteFilePath()
                ? exe.ToAbsoluteFilePath()
                : Cheat.Args.WorkingDirectory.ToAbsoluteDirectoryPath().GetChildFileWithName(exe));
            if (ConfigurationManager.AppSettings["Logentries.Token"] == null)
                ConfigurationManager.AppSettings["Logentries.Token"] = "e0dbaa42-f633-4df4-a6d2-a415c5e49fd0";
            SetupLogging();
            new AssemblyHandler().Register();
            SetupVersion();
            Consts.InternalVersion += $" (runtime: {Api.Version}, engine: {Consts.ProductVersion})";
            Consts.ProductVersion = Api.Version;
            _args = Environment.GetCommandLineArgs().Skip(exe.ContainsIgnoreCase("Electron") ? 2 : 1).ToArray();
            Init();
            if (Api.Args != null)
                Cheat.Args = Api.Args;

            ErrorHandler.Handler = new NodeErrorHandler(Api);
            LaunchAppThread();
        }

        private static void Init() {
            Common.AppCommon.ApplicationName = Consts.InternalTitle; // Used in temp path too.
            MainLog.Logger.Info(
                $"Initializing {Common.AppCommon.ApplicationName} {Consts.ProductVersion} ({Consts.InternalVersion}). Arguments: " +
                _args.CombineParameters());
            Common.IsMini = true;
            Common.ReleaseTitle = Consts.ReleaseTitle;
        }

        public static void Main() => LaunchWithNode();

        static void SetupLogging() {
            SetupNlog.Initialize(Consts.ProductTitle);
            if (Common.Flags.Verbose) {
                var splatLogger = new NLogSplatLogger();
                Locator.CurrentMutable.Register(() => splatLogger, typeof (ILogger));
            }
#if DEBUG
            LogHost.Default.Level = LogLevel.Debug;
#endif
        }
    }
}