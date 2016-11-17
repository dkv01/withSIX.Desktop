// <copyright company="SIX Networks GmbH" file="Entrypoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Wpf;
using withSIX.Core.Presentation.Wpf.Services;
using withSIX.Core.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Presentation.Wpf.Services;

namespace withSIX.Mini.Presentation.Wpf
{
    public static class Entrypoint
    {
        private static string[] _args;
        //static AppBootstrapper _bs;
        private static void SetupVersion() {
            Consts.InternalVersion = CommonBase.AssemblyLoader.GetEntryVersion().ToString();
            Consts.ProductVersion = CommonBase.AssemblyLoader.GetInformationalVersion();
        }

        static void SetupAssemblyLoader() {
            CommonBase.AssemblyLoader = new AssemblyLoader(typeof(Entrypoint).Assembly);
            SetupRegistry();
        }

        static void SetupRegistry() {
            var registry = new AssemblyRegistry(CommonBase.AssemblyLoader.GetNetEntryPath());
            AppDomain.CurrentDomain.AssemblyResolve += registry.CurrentDomain_AssemblyResolve;
        }

        private static void Init() {
            Common.AppCommon.SetAppName(Consts.InternalTitle); // Used in temp path too.
            MainLog.Logger.Info(
                $"Initializing {Common.AppCommon.ApplicationName} {Consts.ProductVersion} ({Consts.InternalVersion}). Arguments: " +
                _args.CombineParameters());
            Common.IsMini = true;
            Common.ReleaseTitle = Consts.ReleaseTitle;
        }

        [STAThread]
        public static void Main() {
            AttachConsole(-1);
            new RuntimeCheckWpf().Check();
            SetupAssemblyLoader();
            new AssemblyHandler().Register();

            Common.Flags = new Common.StartupFlags(_args = Environment.GetCommandLineArgs().Skip(1).ToArray(),
                Environment.Is64BitOperatingSystem);

            LoggingSetup.Setup(Consts.ProductTitle);
            SetupVersion();
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

        static void HandleSquirrel() => new SquirrelUpdater().HandleStartup(_args);

        static int StartApp() {
            var app = new App();
            app.InitializeComponent();
            return app.Run();
        }
    }
}