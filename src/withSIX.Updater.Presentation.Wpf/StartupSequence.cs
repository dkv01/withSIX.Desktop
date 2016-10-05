// <copyright company="SIX Networks GmbH" file="StartupSequence.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using withSIX.Core;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Assemblies;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Bridge.Logging;
using withSIX.Core.Presentation.Wpf.Services;

namespace withSIX.Updater.Presentation.Wpf
{
    public class StartupSequence
    {
        public static void PreInit(string appName) {
            new SplashScreen(@"six_updater_splash.png").Show(false);
            new RuntimeCheckWpf().Check();
            if (ConfigurationManager.AppSettings["Logentries.Token"] == null)
                ConfigurationManager.AppSettings["Logentries.Token"] = "35fdcd29-36a5-4f66-a19f-fe9094d86f72";
            SetupNlog.Initialize(appName);
            new AssemblyHandler().Register();
            Common.App.Init(appName);
        }

        public static void Start(IWpfStartupManager startupManager) {
            startupManager.RegisterServices();
        }

        public static Task Exit(IStartupManager startupManager) => startupManager.Exit();
    }
}