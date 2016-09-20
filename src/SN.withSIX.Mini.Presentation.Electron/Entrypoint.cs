// <copyright company="SIX Networks GmbH" file="Entrypoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SimpleInjector;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Core.Presentation.Assemblies;
using SN.withSIX.Core.Presentation.Logging;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Core.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Presentation.Core;
using Splat;
using withSIX.Api.Models.Extensions;
using ILogger = Splat.ILogger;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public static class Entrypoint
    {
        private static ElectronAppBootstrapper _bootstrapper;
        private static string[] _args;
        //static AppBootstrapper _bs;

        public static INodeApi Api { get; private set; }

        public static async Task MainForNode(INodeApi api) {
            Api = api;
            await LaunchAppThread().ConfigureAwait(false);
        }

        private static Task LaunchAppThread() => TaskExt.StartLongRunningTask(async () => {
            await LaunchWithNode().ConfigureAwait(false);
            _bootstrapper = new ElectronAppBootstrapper(Locator.CurrentMutable, _args);
            await StartupInternal().ConfigureAwait(false);
        });

        private static Task StartupInternal() => _bootstrapper.Startup(() => TaskExt.Default);

        private static void SetupVersion() {
            Consts.InternalVersion = CommonBase.AssemblyLoader.GetEntryVersion().ToString();
            Consts.ProductVersion = CommonBase.AssemblyLoader.GetInformationalVersion();
        }

        public static void ExitForNode() => _bootstrapper.Dispose();

        static void SetupAssemblyLoader(IAbsoluteFilePath locationOverride = null) {
            var entryAssembly = typeof (Entrypoint).Assembly;
            CommonBase.AssemblyLoader = new AssemblyLoader(entryAssembly, locationOverride,
                entryAssembly.Location.ToAbsoluteFilePath().ParentDirectoryPath);
        }

        static void SetupRegistry() {
            var registry = new AssemblyRegistry();
            AppDomain.CurrentDomain.AssemblyResolve += registry.CurrentDomain_AssemblyResolve;
        }

        static async Task LaunchWithNode() {
            await new RuntimeCheckNode().Check().ConfigureAwait(false);

            SetupRegistry();

            var cla = Environment.GetCommandLineArgs();
            var exe = cla.First();
            if (!exe.EndsWith(".exe"))
                exe = exe + ".exe";
            Common.Flags = new Common.StartupFlags(_args = cla.Skip(exe.ContainsIgnoreCase("Electron") ? 2 : 1).ToArray());
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
            Init();
            if (Api.Args != null)
                Cheat.Args = Api.Args;

            ErrorHandler.Handler = new NodeErrorHandler(Api);
        }

        private static void Init() {
            Common.AppCommon.SetAppName(Consts.InternalTitle); // Used in temp path too.
            MainLog.Logger.Info(
                $"Initializing {Common.AppCommon.ApplicationName} {Consts.ProductVersion} ({Consts.InternalVersion}). Arguments: " +
                _args.CombineParameters());
            Common.IsMini = true;
            Common.ReleaseTitle = Consts.ReleaseTitle;
        }

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

        public class RuntimeCheckNode : RuntimeCheck
        {
            protected override async Task<bool> FatalErrorMessage(string message, string caption)
                => (await Api.ShowMessageBox(caption, message, new[] {"Yes", "No"}).ConfigureAwait(false)) == "Yes";
        }
    }
}