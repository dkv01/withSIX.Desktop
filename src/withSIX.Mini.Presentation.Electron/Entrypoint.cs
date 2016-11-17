// <copyright company="SIX Networks GmbH" file="Entrypoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Services;
using withSIX.Mini.Applications;

namespace withSIX.Mini.Presentation.Electron
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
            var entryAssembly = typeof(Entrypoint).GetTypeInfo().Assembly;
            var entryPath = entryAssembly.Location.ToAbsoluteFilePath().ParentDirectoryPath;
            _bootstrapper = new ElectronAppBootstrapper(_args, entryPath);
            _bootstrapper.Configure();
            await StartupInternal().ConfigureAwait(false);
        });

        private static Task StartupInternal() => _bootstrapper.Startup(() => TaskExt.Default);

        private static void SetupVersion() {
            Consts.InternalVersion = CommonBase.AssemblyLoader.GetEntryVersion().ToString();
            Consts.ProductVersion = CommonBase.AssemblyLoader.GetInformationalVersion();
        }

        public static void ExitForNode() => _bootstrapper.Dispose();

        static void SetupAssemblyLoader(IAbsoluteFilePath locationOverride = null) {
            var entryAssembly = typeof(Entrypoint).GetTypeInfo().Assembly;
            var entryPath = entryAssembly.Location.ToAbsoluteFilePath().ParentDirectoryPath; 
            CommonBase.AssemblyLoader = new AssemblyLoader(entryAssembly, locationOverride, entryPath);
            SetupRegistry();
        }

        static void SetupRegistry() {
            var registry = new AssemblyRegistry();
            AppDomain.CurrentDomain.AssemblyResolve += registry.CurrentDomain_AssemblyResolve;
        }

        static async Task LaunchWithNode() {
            await new RuntimeCheckNode().Check().ConfigureAwait(false);

            var cla = Environment.GetCommandLineArgs();
            var exe = cla.First();
            if (!exe.EndsWith(".exe"))
                exe = exe + ".exe";
            SetupAssemblyLoader(exe.IsValidAbsoluteFilePath()
                ? exe.ToAbsoluteFilePath()
                : Cheat.Args.WorkingDirectory.ToAbsoluteDirectoryPath().GetChildFileWithName(exe));

            Common.Flags =
                new Common.StartupFlags(_args = cla.Skip(exe.ContainsIgnoreCase("Electron") ? 2 : 1).ToArray(),
                    Environment.Is64BitOperatingSystem);
            SetupLogging("e0dbaa42-f633-4df4-a6d2-a415c5e49fd0");
            new AssemblyHandler().Register();
            SetupVersion();
            Consts.InternalVersion += $" (runtime: {Api.Version}, engine: {Consts.ProductVersion})";
            Consts.ProductVersion = Api.Version;
            Init();
            if (Api.Args != null)
                Cheat.Args = Api.Args;

            ErrorHandlerCheat.Handler = new NodeErrorHandler(Api);
        }

        private static void Init() {
            Common.AppCommon.SetAppName(Consts.InternalTitle); // Used in temp path too.
            MainLog.Logger.Info(
                $"Initializing {Common.AppCommon.ApplicationName} {Consts.ProductVersion} ({Consts.InternalVersion}). Arguments: " +
                _args.CombineParameters());
            Common.IsMini = true;
            Common.ReleaseTitle = Consts.ReleaseTitle;
        }

        static void SetupLogging(string token) {
            LoggingSetup.Setup(Consts.ProductTitle, token);
        }

        public class RuntimeCheckNode : RuntimeCheck
        {
            protected override async Task<bool> FatalErrorMessage(string message, string caption)
                => await Api.ShowMessageBox(caption, message, new[] {"Yes", "No"}).ConfigureAwait(false) == "Yes";
        }
    }
}