// <copyright company="SIX Networks GmbH" file="Entrypoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Assemblies;
using SN.withSIX.Core.Presentation.Logging;
using SN.withSIX.Core.Presentation.Wpf;
using SN.withSIX.Core.Presentation.Wpf.Services;
using SN.withSIX.Core.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Presentation.Core;
using SN.withSIX.Mini.Presentation.Wpf.Services;
using Splat;
using ILogger = Splat.ILogger;

namespace SN.withSIX.Mini.Presentation.Wpf
{
    public static class Entrypoint
    {
        private static string[] _args;
        //static AppBootstrapper _bs;
        private static void SetupVersion() {
            Consts.InternalVersion = CommonBase.AssemblyLoader.GetEntryVersion().ToString();
            Consts.ProductVersion = CommonBase.AssemblyLoader.GetInformationalVersion();
        }

        static void SetupAssemblyLoader() => CommonBase.AssemblyLoader = new AssemblyLoader(typeof (Entrypoint).Assembly);

        static void SetupRegistry() {
            var registry = new AssemblyRegistry();
            AppDomain.CurrentDomain.AssemblyResolve += registry.CurrentDomain_AssemblyResolve;
        }

        private static void Init() {
            Common.AppCommon.ApplicationName = Consts.InternalTitle; // Used in temp path too.
            MainLog.Logger.Info(
                $"Initializing {Common.AppCommon.ApplicationName} {Consts.ProductVersion} ({Consts.InternalVersion}). Arguments: " +
                _args.CombineParameters());
            Common.IsMini = true;
            Common.ReleaseTitle = Consts.ReleaseTitle;
        }

        [STAThread]
        public static void Main() {
            AttachConsole(-1);

            SetupRegistry();
            new RuntimeCheckWpf().Check();
            SetupAssemblyLoader();
            SetupLogging();
            new AssemblyHandler().Register();
            SetupVersion();
            _args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            Init();
            //Cheat.Args = new ArgsO { Port =, WorkingDirectory = Directory.GetCurrentDirectory() } // todo;
            try {
                HandleSquirrel();
            } catch (Exception ex) {
                MainLog.Logger.FormattedErrorException(ex, "An error occurred during processing startup");
                throw;
            }
            StartApp();
        }

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

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

        static void HandleSquirrel() => new SquirrelUpdater().HandleStartup(_args);

        static int StartApp() {
            var app = new App();
            app.InitializeComponent();
            return app.Run();
        }
    }
}