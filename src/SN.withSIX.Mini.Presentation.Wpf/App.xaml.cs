// <copyright company="SIX Networks GmbH" file="App.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SN.withSIX.Core.Presentation.Wpf;
using SN.withSIX.Core.Presentation.Wpf.Extensions;
using SN.withSIX.Core.Presentation.Wpf.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Presentation.Core;
using Splat;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Wpf
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : SingleInstanceApp
    {
        private readonly SIHandler _siHandler;
        WpfAppBootstrapper _bootstrapper;
        private CMBootstrapper _cmBs;

        public App() {
            AppEvent += OnAppEvent;
            _siHandler = new SIHandler();
#if !DEBUG
            ExDialog.SetupExceptionHandler(this);
#endif
        }

        void OnAppEvent(object sender, IList<string> list) {
            var mainWindow = MainWindow;
            if (mainWindow != null) {
                mainWindow.Show();
                mainWindow.Activate();
            }
            HandleNewVersion(list);
            Task.Run(() => _siHandler.HandleSingleInstanceCall(list.ToList()));
        }

        void HandleNewVersion(IEnumerable<string> list) {
            if (list.Select(a => new {a, newversion = "-NewVersion="})
                .Where(t => t.a.StartsWith(t.newversion))
                .Select(t => t.a.Replace(t.newversion, ""))
                .Any(newVer => newVer != Consts.ProductVersion)) {
                var mainWindow = MainWindow;
                var text = "You tried to start a different version, please exit the current version and try again";
                var title = "Started another version";
                if (mainWindow == null)
                    MessageBox.Show(text, title);
                else
                    MessageBox.Show(mainWindow, text, title);
            }
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            _bootstrapper = new WpfAppBootstrapper(Environment.GetCommandLineArgs().Skip(1).ToArray());
            _bootstrapper.Configure();
            if (_bootstrapper.CommandMode)
                HandleSingleInstance();
            _cmBs = new CMBootstrapper(_bootstrapper);
            TaskExt.StartLongRunningTask(StartupInternal).WaitSpecial();
        }

        static void HandleSingleInstance() {
            if (
                    !SingleInstance<App>.TryInitializeAsFirstInstance<App>("withSIX-Sync",
                        new[] {"-NewVersion=" + Consts.ProductVersion},
                        Path.GetFileName(Assembly.GetEntryAssembly().Location)))
                // TODO; Deal with 'another version'
                Environment.Exit(0);
        }


        // TODO: In command mode perhaps we shouldnt even run a WpfApp instance or ??
        private Task StartupInternal() => _bootstrapper.Startup(async () => { await CreateMainWindow(); });

        DispatcherOperation CreateMainWindow() {
            var miniVm = _bootstrapper.GetMainWindowViewModel();
            return Current.Dispatcher.InvokeAsync(() => {
                var miniMainWindow = new MiniMainWindow {ViewModel = miniVm};
                miniMainWindow.Show();
                if (!Environment.GetCommandLineArgs().Contains("--hide"))
                    miniVm.OpenPopup.Execute(null);
            });
            //MainWindow = miniMainWindow;
        }

        protected override void OnExit(ExitEventArgs e) {
            base.OnExit(e);
            try {
                _bootstrapper?.Dispose();
            } finally {
                SingleInstance<App>.Cleanup();
            }
        }
    }
}