// <copyright company="SIX Networks GmbH" file="StartupSequence.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NDepend.Path;
using ReactiveUI;
using withSIX.Core;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.MVVM.ViewModels.Dialogs;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Bridge.Logging;
using withSIX.Core.Presentation.Wpf.Services;
using withSIX.Play.Applications;

namespace withSIX.Play.Presentation.Wpf
{
    // WARNING - DO NOT SHOW NON-FATAL DIALOGS HERE
    public static class StartupSequence
    {
        public static void PreInit(string appName) {
            CheckEnvironment();
            InitializeEnvironment(appName);
            HandlePortable();
        }

        static void CheckEnvironment() {
            new RuntimeCheckWpf().Check();
            // Has to happen here because of otherwise not automatically picking up after install on first launch. on production machines.
        }

        static void InitializeEnvironment(string appName) {
            if (ConfigurationManager.AppSettings["Logentries.Token"] == null)
                ConfigurationManager.AppSettings["Logentries.Token"] = "35fdcd29-36a5-4f66-a19f-fe9094d86f72";

            SetupNlog.Initialize(appName);
            new SplashScreen(@"app.png").Show(true);
            new AssemblyHandler().Register();
            Common.App.Init(appName);
        }

        public static void Start(IPlayStartupManager startupManager) {
            startupManager.HandleSoftwareUpdate();
            startupManager.ClearAwesomiumCache();
            startupManager.StartAwesomium();
            startupManager.FirstTimeLicenseDialog(new FirstTimeRunDialogViewModel());
            startupManager.RegisterServices();
            RegisterUrlHandlers(startupManager);
            RegisterUserAppKeys(startupManager);
            //startupManager.RegisterOnline();
            startupManager.LaunchSignalr();
        }

        static void RegisterUserAppKeys(IPlayStartupManager startupManager) {
            try {
                startupManager.RegisterUserAppKeys();
            } catch (UnauthorizedAccessException e) {
                MainLog.Logger.FormattedWarnException(e);
            } catch (Exception e) {
                UserErrorHandler.HandleUserError(new InformationalUserError(e, "failed to register user app keys", null));
            }
        }

        static void RegisterUrlHandlers(IPlayStartupManager startupManager) {
            try {
                startupManager.RegisterUrlHandlers();
            } catch (UnauthorizedAccessException e) {
                MainLog.Logger.FormattedWarnException(e);
            } catch (Exception e) {
                UserErrorHandler.HandleUserError(new InformationalUserError(e, "failed to register user handlers", null));
            }
        }

        public static Task Exit(IStartupManager startupManager) => startupManager.Exit();

        static void HandlePortable() {
            if (!Common.Flags.Portable)
                return;
            var appPath = CommonBase.AssemblyLoader.GetEntryPath();
            var dataPath = appPath.GetChildDirectoryWithName("Data");
            if (dataPath.Exists)
                return;

            var oldDataPath =
                appPath.ParentDirectoryPath.DirectoryInfo.GetDirectories()
                    .Reverse()
                    .Select(dir => dir.FullName.ToAbsoluteDirectoryPath().GetChildDirectoryWithName("Data"))
                    .FirstOrDefault(p => p.Exists);
            if (oldDataPath != null)
                Tools.FileUtil.Ops.CopyDirectory(oldDataPath, dataPath);
        }
    }
}