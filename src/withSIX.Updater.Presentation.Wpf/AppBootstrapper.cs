// <copyright company="SIX Networks GmbH" file="AppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Caliburn.Micro;
using withSIX.Core;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Infrastructure;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Extensions;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Presentation.Wpf.Legacy;
using withSIX.Core.Presentation.Wpf.Views.Dialogs;
using withSIX.Play.Applications.Services;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core;
using withSIX.Play.Core.Options;
using withSIX.Play.Infra.Data.Services;
using withSIX.Sync.Core.Repositories;
using withSIX.Sync.Core.Transfer;
using withSIX.Updater.Presentation.Wpf.Commands;
using withSIX.Api.Models.Extensions;

namespace withSIX.Updater.Presentation.Wpf
{
    public class AppBootstrapper : WpfAppBootstrapper<IShellViewModel>
    {
        IWpfStartupManager _startupManager;

        public AppBootstrapper() {
            DisplayRootView = false;
        }

        protected override void PreStart() {
            _startupManager = IoC.Get<IWpfStartupManager>();
            StartupSequence.Start(_startupManager);
            Environment.Exit(IoC.Get<SixElevatedService>().Run(Common.Flags.FullStartupParameters));
        }


        protected override async Task ExitAsync(object sender, EventArgs eventArgs) {
            await base.ExitAsync(sender, eventArgs).ConfigureAwait(false);
            if (_startupManager != null)
                await Task.Run(() => StartupSequence.Exit(_startupManager)).ConfigureAwait(false);
            else
                MainLog.Logger.Error("StartupManager was null on exit!");
        }

        protected override void ConfigureContainer() {
            base.ConfigureContainer();
            Container.Register<IWpfStartupManager, WpfStartupManager>();
            Container.Register<IExceptionHandler, DefaultExceptionHandler>();
            Container.RegisterPlugins<BaseCommand>(AssemblySource.Instance); // TODO
            Container.RegisterSingleton<SixElevatedService>();

            Container.RegisterSingleton(() => DomainEvilGlobal.Settings);
            Container.RegisterSingleton(() => DomainEvilGlobal.LocalMachineInfo);

            Container.RegisterSingleton<IUserSettingsStorage>(
                () =>
                    new UserSettingsStorage(
                        (ex, location) => Container.GetInstance<IDialogManager>().ExceptionDialog(ex,
                            $"An error occurred while trying to load Settings from: {location}\nIf you continue you will loose your settings, but we will at least make a backup for you.")));

            Container.RegisterSingleton<Func<ProtocolPreference>>(
                () => Container.GetInstance<UserSettings>().AppOptions.ProtocolPreference);
            Container
                .RegisterSingleton<Func<IMultiMirrorFileDownloader, ExportLifetimeContext<IFileQueueDownloader>>>(
                    x => {
                        var appOptions = Container.GetInstance<UserSettings>().AppOptions;
                        var downloader = new MultiThreadedFileQueueDownloader(appOptions.GetMaxThreads, x);
                        return new ExportLifetimeContext<IFileQueueDownloader>(downloader, TaskExt.NullAction);
                    });
        }

        protected override void AfterSetup() {
            base.AfterSetup();
            DomainEvilGlobal.SecretData = Task.Run(() => BuildSecretData()).Result;
            DomainEvilGlobal.Settings = Container.GetInstance<IUserSettingsStorage>().TryLoadSettings();
        }

        async Task<SecretData> BuildSecretData() {
            var cm = Container.GetInstance<ISecureCacheManager>();
            var authKey = "Authentication";
            var userInfoKey = "UserInfo";

            var secretData = new SecretData {
                Authentication = await cm.GetOrCreateObject(authKey, () => new AuthenticationData()),
                UserInfo = await cm.GetOrCreateObject(userInfoKey, () => new UserInfo())
            };
            secretData.Save = async () => {
                await cm.SetObject(authKey, secretData.Authentication);
                await cm.SetObject(userInfoKey, secretData.UserInfo);
            };
            return secretData;
        }

        protected override IEnumerable<Assembly> SelectAssemblies() => new[] {
                Assembly.GetExecutingAssembly(),
                typeof (EnterConfirmView).Assembly,
                typeof (StartupManager).Assembly,
                typeof (UserSettingsStorage).Assembly,
                typeof (SystemInfo).Assembly,
                typeof (ScreenBase).Assembly,
                typeof (ToolsInstaller).Assembly,
                typeof (Repository).Assembly,
                typeof (WindowManager).Assembly
            }.Concat(base.SelectAssemblies()).Distinct();
    }
}