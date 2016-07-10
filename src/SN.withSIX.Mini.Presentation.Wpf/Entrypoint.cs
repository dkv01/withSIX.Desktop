// <copyright company="SIX Networks GmbH" file="Entrypoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Assemblies;
using SN.withSIX.Core.Presentation.SA;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Core.Presentation.Wpf;
using SN.withSIX.Core.Presentation.Wpf.Extensions;
using SN.withSIX.Core.Presentation.Wpf.Services;
using SN.withSIX.Core.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Presentation.Core;
using SN.withSIX.Mini.Presentation.Electron;
using SN.withSIX.Mini.Presentation.Wpf.Services;
using Splat;
using ILogger = Splat.ILogger;

namespace SN.withSIX.Mini.Presentation.Wpf
{
    public static class Entrypoint
    {
        //static AppBootstrapper _bs;
        public static bool CommandMode { get; private set; }

        private static void SetupVersion() {
            Consts.InternalVersion = CommonBase.AssemblyLoader.GetEntryVersion().ToString();
            Consts.ProductVersion = CommonBase.AssemblyLoader.GetInformationalVersion();
        }

        static void SetupAssemblyLoader(IAbsoluteFilePath locationOverride = null) {
            CommonBase.AssemblyLoader = new AssemblyLoader(typeof (Entrypoint).Assembly, locationOverride);
        }

        static void SetupRegistry() {
            var registry = new AssemblyRegistry();
            AppDomain.CurrentDomain.AssemblyResolve += registry.CurrentDomain_AssemblyResolve;
        }

        private static void Init(string arguments) {
            ExceptionExtensions.FormatException = KnownExceptions.FormatException;
            Common.AppCommon.ApplicationName = Consts.InternalTitle; // Used in temp path too.
            MainLog.Logger.Info(
                $"Initializing {Common.AppCommon.ApplicationName} {Consts.ProductVersion} ({Consts.InternalVersion}). Arguments: " +
                arguments);
            Common.IsMini = true;
            Common.ReleaseTitle = Consts.ReleaseTitle;
        }

        [STAThread]
        public static void Main() => LaunchWithoutNode();

        private static void LaunchWithoutNode() {
            SetupRegistry();
            new RuntimeCheckWpf().Check();
            SetupAssemblyLoader();
            SetupLogging();
            new AssemblyHandler().Register();
            SetupVersion();
            var arguments = Environment.GetCommandLineArgs().Skip(1).ToArray();
            Init(arguments.CombineParameters());
            //Cheat.Args = new ArgsO { Port =, WorkingDirectory = Directory.GetCurrentDirectory() } // todo;
            HandleCommandMode(arguments);
            try {
                HandleSquirrel(arguments);
            } catch (Exception ex) {
                MainLog.Logger.FormattedErrorException(ex, "An error occurred during processing startup");
                throw;
            }
            if (!CommandMode)
                HandleSingleInstance();
            StartApp();
        }

        static void HandleCommandMode(string[] arguments) {
            if (arguments.Any()) {
                var firstArgument = arguments.First();
                if (!firstArgument.StartsWith("-") && !firstArgument.StartsWith("syncws://"))
                    CommandMode = true;
            }
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

        static void HandleSquirrel(IReadOnlyCollection<string> arguments)
            => new SquirrelUpdater().HandleStartup(arguments);

        static void HandleSingleInstance() {
            if (
                !SingleInstance<App>.TryInitializeAsFirstInstance<App>("withSIX-Sync",
                    new[] {"-NewVersion=" + Consts.ProductVersion},
                    Path.GetFileName(Assembly.GetEntryAssembly().Location)))
                // TODO; Deal with 'another version'
                Environment.Exit(0);
        }

        static int StartApp() {
            var app = new App();
            app.InitializeComponent();
            return app.Run();
        }
    }
}