// <copyright company="SIX Networks GmbH" file="StartupSequence.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation;
using withSIX.Core.Presentation.Assemblies;
using withSIX.Core.Presentation.Logging;
using withSIX.Core.Services;

namespace withSIX.Sync.Presentation.Console
{
    // WARNING - DO NOT SHOW NON-FATAL DIALOGS HERE
    public static class StartupSequence
    {
        public static void PreInit(string appName) {
            CommonBase.AssemblyLoader = new AssemblyLoader(Assembly.GetEntryAssembly());
            new RuntimeCheck().Check();
            if (ConfigurationManager.AppSettings["Logentries.Token"] == null)
                ConfigurationManager.AppSettings["Logentries.Token"] = "cae22f74-4cd1-41c0-88c1-d0c836b079bb";
            SetupNlog.Initialize(appName);
            new AssemblyHandler().Register();
            Common.App.Init(appName);
        }

        public static void Start(IStartupManager startupManager) {
            startupManager.RegisterServices();
        }

        public static Task Exit(IStartupManager startupManager) => startupManager.Exit();
    }
}