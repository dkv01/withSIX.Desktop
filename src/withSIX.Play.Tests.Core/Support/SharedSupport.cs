// <copyright company="SIX Networks GmbH" file="SharedSupport.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using Caliburn.Micro;
using FakeItEasy;
using NDepend.Path;
using NLog;
using NLog.Config;
using MediatR;
using withSIX.Core;
using withSIX.Core.Infra.Services;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Bridge.Logging;
using withSIX.Core.Presentation.Bridge.Services;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Play.Applications;
using withSIX.Play.Core;
using withSIX.Play.Core.Options;
using withSIX.Sync.Core.Transfer;
using Action = System.Action;

namespace withSIX.Play.Tests.Core.Support
{
    public static class SharedSupport
    {
        // All shared initialization
        public static void Init() {
            SimpleConfigurator.ConfigureForConsoleLogging(LogLevel.Info); // Doesnt seem to work anymore?!
            SetupNlog.Initialize("Test Runner");
            CommonBase.AssemblyLoader = GetAssemblyLoader();
            // Use to Reset the various common instances
            // Normally the EA instance is created in the AppBootStrapper, and Dependency injected into ShellViewModel
            Common.App = new Common.AppCommon();
            Common.Paths = new PathConfiguration();
            // Must set AppPath to curdir, otherwise we end up somewhere in test heaven
            // Also keep the test configuration and temp data separate from production
            Common.App.InitLocalWithCleanup("Test Runner");
            var ea = new EventAggregator();
            Cheat.SetServices(new CheatImpl(ea, new Mediator(null, null)));
            DomainEvilGlobal.Settings = new UserSettings();
            /*
            Tools.RegisterServices(new ToolsServices(new ProcessManager(),
                new Lazy<IWCFClient>(() => new WCFClient()),
                new Lazy<IGeoIpService>(() => new GeoIpService()), new CompressionUtil()));
                */
            ReSetupTools();

            if (!SingleSetup) {
                new AssemblyHandler().Register();
                SingleSetup = true;
            }
        }

        static bool SingleSetup;

        public static ExportLifetimeContext<IWebClient> CreateWebClientExportFactory() => null; //new ExportLifetimeContext<IWebClient>(OnExportLifetimeContextCreator);

        public static ExportLifetimeContext<IWebClient> CreateFakeWebClientExportFactory(IWebClient webClient = null)
            => null; // new ExportLifetimeContext<IWebClient>(() => OnExportLifetimeFakeContextCreator(webClient));

        static Tuple<IWebClient, Action> OnExportLifetimeFakeContextCreator(IWebClient webClient = null) {
            if (webClient == null)
                webClient = A.Fake<IWebClient>();
            return new Tuple<IWebClient, Action>(webClient, webClient.Dispose);
        }

        static Tuple<IWebClient, Action> OnExportLifetimeContextCreator() {
            var wc = new WebClient();
            return new Tuple<IWebClient, Action>(wc, wc.Dispose);
        }

        static void ReSetupTools() {
            Tools.Generic = new Tools.GenericTools();
            Tools.HashEncryption = new Tools.HashEncryptionTools();
            Tools.Transfer = new Tools.TransferTools();
        }

        public static void HandleTools() {
            var curDir = Directory.GetCurrentDirectory();
            var localData = Path.Combine(curDir, "LocalData");
            var solutionDir = new DirectoryInfo(curDir).Parent.Parent.Parent.FullName; // Solution/Project/bin/debug

            Tools.FileUtil.Ops.CopyDirectoryWithRetry(Path.Combine(solutionDir, "tools").ToAbsoluteDirectoryPath(),
                Path.Combine(localData, "tools").ToAbsoluteDirectoryPath());
        }

        public static IAssemblyLoader GetAssemblyLoader() {
            //When class used Assembly.GetEntryAssembly() run in unit test, the Assembly.GetEntryAssembly() is null.
            var loader = A.Fake<IAssemblyLoader>();
            var scAs = Assembly.LoadFrom("withSIX.Core.dll");
            A.CallTo(() => loader.GetEntryLocation())
                .Returns(scAs.Location.ToAbsoluteFilePath());
            A.CallTo(() => loader.GetEntryPath())
                .Returns(loader.GetEntryLocation().ParentDirectoryPath);

            return loader;
        }
    }
}