// <copyright company="SIX Networks GmbH" file="WpfAppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using NDepend.Path;
using Newtonsoft.Json;
using ReactiveUI;
using SimpleInjector;
using Splat;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Bridge.Extensions;
using withSIX.Core.Presentation.Legacy;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Presentation.Wpf;
using withSIX.Core.Presentation.Wpf.Extensions;
using withSIX.Core.Presentation.Wpf.Legacy;
using withSIX.Core.Presentation.Wpf.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.MVVM.Features;
using withSIX.Mini.Applications.MVVM.ViewModels;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Infra.Api;
using withSIX.Mini.Infra.Data;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Wpf.Services;
using DefaultViewLocator = withSIX.Mini.Presentation.Wpf.Services.DefaultViewLocator;
using IScreen = Caliburn.Micro.IScreen;
using ViewLocator = Caliburn.Micro.ViewLocator;

namespace withSIX.Mini.Presentation.Wpf
{
    public class WorkaroundBootstrapper : AppBootstrapper
    {
        protected WorkaroundBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath) : base(args, rootPath) {}

        protected override void LowInitializer() => BootstrapperBridge.LowInitializer();
        protected override void RegisterPlugins<T>(IEnumerable<Assembly> assemblies, Lifestyle style = null) => Container.RegisterPlugins<T>(assemblies, style);
        protected override IEnumerable<Type> GetTypes<T>(IEnumerable<Assembly> assemblies) => assemblies.GetTypes<T>();
        protected override void RegisterAllInterfaces<T>(IEnumerable<Assembly> assemblies) => Container.RegisterAllInterfaces<T>(assemblies);
        protected override void RegisterSingleAllInterfaces<T>(IEnumerable<Assembly> assemblies) => Container.RegisterSingleAllInterfaces<T>(assemblies);
        protected override IEnumerable<Assembly> GetInfraAssemblies => new[] {typeof(AutoMapperInfraDataConfig).GetTypeInfo().Assembly, typeof(StateHandler).GetTypeInfo().Assembly }.Concat(base.GetInfraAssemblies);
        protected override Assembly AssemblyLoadFrom(string arg) => BootstrapperBridge.AssemblyLoadFrom(arg);
        protected override void ConfigureContainer() => BootstrapperBridge.ConfigureContainer(Container);
        protected override void EnvironmentExit(int exitCode) => BootstrapperBridge.EnvironmentExit(exitCode);
        protected override void RegisterMessageBus() => BootstrapperBridge.RegisterMessageBus(Container);
    }

    class WpfAppBootstrapper : WorkaroundBootstrapper, ICMBootStrapper<string>
    {
        IMiniMainWindowViewModel _mainVm;

        internal WpfAppBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath) : base(args, rootPath) {
            SIHandler.ParseQueryString = (query) => {
                var nameValueCollection = HttpUtility.ParseQueryString(query);
                return nameValueCollection.Cast<string>().ToDictionary(x => x, x => nameValueCollection.GetValues(x));
            };
            NDepend.Helpers.NDepend.Helpers.Workaround.IsNormalizedFunc = x => x.IsNormalized();
        }

        public override void Configure() {
            base.Configure();
            SetupRx();
            SetupCM();
            Locator.CurrentMutable.Register(() => new JsonSerializerSettings().SetDefaultConverters(),
                typeof(JsonSerializerSettings));
        }

        internal IMiniMainWindowViewModel GetMainWindowViewModel() => Container.GetInstance<IMiniMainWindowViewModel>();

        void SetupRx() {
            //SetupRxUiDepResolver();
            var viewLocator = new DefaultViewLocator();
            // If we use the withSIX.Core.Presentation.Wpf.Services. one then we get Reactivecommands as text etc..
            //var jsonSerializerSettings = new JsonSerializerSettings() { DateTimeZoneHandling = DateTimeZoneHandling.Utc };
            Locator.CurrentMutable.Register(() => viewLocator, typeof(IViewLocator));
            //_dependencyResolver.Register(() => jsonSerializerSettings, typeof (JsonSerializerSettings));
        }


        void SetupRxUiDepResolver() {
            var dependencyResolver = new SimpleInjectorMutableDependencyResolver(Container, Locator.CurrentMutable);
            Locator.CurrentMutable = dependencyResolver;
            Locator.Current = dependencyResolver;
            dependencyResolver.InitializeSplat();
            dependencyResolver.InitializeReactiveUI();
        }

        public static void RegisterEventAggregator(Container container) {
            container.RegisterSingleton(new EventAggregator());
            container.RegisterSingleton<IEventAggregator>(container.GetInstance<EventAggregator>);
            container.RegisterInitializer<IHandle>(x => container.GetInstance<IEventAggregator>().Subscribe(x));
        }

        protected override void RegisterServices() {
            base.RegisterServices();
            Container.RegisterSingleton<IDialogManager, WpfDialogManager>();
            Container.RegisterSingleton<IWindowManager, CustomWindowManager>();
            Container.RegisterSingleton<IShutdownHandler, WpfShutdownHandler>();

            // Legacy
            RegisterEventAggregator(Container);
        }

        protected override async Task PostInitialize() {
            await base.PostInitialize().ConfigureAwait(false);
            await HandlemainVm().ConfigureAwait(false);
        }

        [Obsolete("TODO: Remove CM ViewLocation with RXUI everywhere")]
        void SetupCM() {
            var original = ViewLocator.LocateForModel;
            ViewLocator.LocateForModel = (o, dependencyObject, arg3) => {
                var v = original(o, dependencyObject, arg3);
                // TODO: Lacks CM's Context/target support
                if ((v == null) || v is TextBlock) {
                    var rxv = (UIElement) ReactiveUI.ViewLocator.Current.ResolveView(o);
                    if (rxv != null)
                        v = rxv;
                }

                var vFor = v as IViewFor;
                if (vFor != null)
                    vFor.ViewModel = o;

                return v;
            };
            //ViewLocator.AddNamespaceMapping("withSIX.Core.Presentation.Wpf.Views", "withSIX.Core.Presentation.Wpf.Views");
            ViewLocator.AddNamespaceMapping("withSIX.Core.Applications.MVVM.ViewModels.Popups",
                "withSIX.Core.Presentation.Wpf.Views.Popups");
            ViewLocator.AddNamespaceMapping("withSIX.Core.Applications.MVVM.ViewModels.Dialogs",
                "withSIX.Core.Presentation.Wpf.Views.Dialogs");
            ViewLocator.AddNamespaceMapping("withSIX.Core.Applications.MVVM.ViewModels",
                "withSIX.Core.Presentation.Wpf.Views");
        }

        public void InitializeCM() {
            UiTaskHandler.RegisterCommand = (command, action) => {
                // ThrownExceptions does not listen to Subscribe errors, but only in async task errors!
                command.ThrownExceptions
                    .Select(x => ErrorHandlerr.HandleException(x, action))
                    .SelectMany(UserErrorHandler.HandleUserError)
                    .Where(x => x == RecoveryOptionResultModel.RetryOperation)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .InvokeCommand(command);
            };
            RxApp.SupportsRangeNotifications = false; // WPF doesnt :/
            SimpleInjectorContainerExtensions.RegisterReserved(typeof(IHandle), typeof(IScreen));
            SimpleInjectorContainerExtensions.RegisterReservedRoot(typeof(IHandle));
        }
        public bool DisplayRootView { get; }

        public void DisplayRootView2() {}

        async Task HandlemainVm() => _mainVm = await new GetMiniMain().ExecuteWrapped().ConfigureAwait(false);

        protected override void RegisterViews() {
            base.RegisterViews();
            var viewInterfaceFilterType = typeof(IViewFor);
            Locator.CurrentMutable.RegisterAllInterfaces<IViewFor>(GetPresentationAssemblies(),
                (type, type1) => viewInterfaceFilterType.IsAssignableFrom(type));
            //dependencyResolver.RegisterConstant(this, typeof (IScreen));

            // TODO: We might still want to leverage S.I also for RXUI View resolution, so that Views may import (presentation) services?
            //_container.RegisterPlugins<ISettingsTabViewModel>(_applicationAssemblies);

            /*
            _container.RegisterAllInterfaces<IViewFor>(_presentationAssemblies, (type, type1) => viewInterfaceFilterType.IsAssignableFrom(type));
            dependencyResolver.Register(container.GetInstance<SettingsWindow>, typeof (IViewFor<SettingsViewModel>));

            //dependencyResolver.Register(container.GetInstance<SettingsView>, typeof (IViewFor<SettingsViewModel>));
            dependencyResolver.Register(container.GetInstance<WelcomeView>, typeof (IViewFor<WelcomeViewModel>));
            dependencyResolver.Register(container.GetInstance<NotificationsView>, typeof (IViewFor<NotificationsViewModel>));
            dependencyResolver.Register(container.GetInstance<DownloadsView>, typeof(IViewFor<DownloadsViewModel>));
            dependencyResolver.Register(container.GetInstance<LibraryView>, typeof (IViewFor<LibraryViewModel>));

            dependencyResolver.Register(container.GetInstance<NotificationView>, typeof(IViewFor<NotificationViewModel>));

            dependencyResolver.Register(container.GetInstance<GameTabView>, typeof(IViewFor<GameTabViewModel>));
            dependencyResolver.Register(container.GetInstance<GameSettingsWindow>, typeof(IViewFor<GameSettingsViewModel>));
            dependencyResolver.Register(container.GetInstance<GameSettingsWindow>, typeof(IViewFor<ArmaSettingsViewModel>)); // , "arma"

            dependencyResolver.Register(container.GetInstance<AccountSettingsTabView>,
                typeof(IAccountSettingsTabView));
            dependencyResolver.Register(container.GetInstance<DownloadSettingsTabView>,
                typeof (IViewFor<DownloadSettingsTabViewModel>));
            dependencyResolver.Register(container.GetInstance<InterfaceSettingsTabView>,
                typeof (IViewFor<InterfaceSettingsTabViewModel>));
            dependencyResolver.Register(container.GetInstance<NotificationSettingsTabView>,
                typeof (IViewFor<NotificationSettingsTabViewModel>));
*/
            Container.RegisterSingleton(() => _mainVm);
            Container.RegisterSingleton<ReactiveUI.IScreen>(Container.GetInstance<IMiniMainWindowViewModel>);
        }

        protected override void BackgroundActions() {
            base.BackgroundActions();
            HandleUpdateStateInBackground();
        }

        private void HandleUpdateStateInBackground()
            => TaskExt.StartLongRunningTask(Container.GetInstance<SelfUpdateHandler>().HandleUpdateState);

        protected override IEnumerable<Assembly> GetApplicationAssemblies()
            =>
            base.GetApplicationAssemblies()
                .Concat(new[] {typeof(IWpfStartupManager).Assembly, typeof(GetMiniMain).Assembly});

        protected override IEnumerable<Assembly> GetPresentationAssemblies()
            =>
            new[] {typeof(App).Assembly}.Concat(
                base.GetPresentationAssemblies().Concat(new[] {typeof(SingleInstanceApp).Assembly}));

        protected override void ConfigureUiInstances() {
            base.ConfigureUiInstances();
            Cache.ImageFiles = Container.GetInstance<Cache.IImageFileCache>();
            UiRoot.Main = new UiRoot(Container.GetInstance<IDialogManager>(),
                Container.GetInstance<ISpecialDialogManager>());
        }

        protected override void EndOv() => End().WaitSpecial();

        private void TryInstallFlashInBackground() => TaskExt.StartLongRunningTask(InstallFlash);

        private static Task InstallFlash() => new FlashHandler(CommonUrls.FlashUri).InstallFlash();
    }

    class SelfUpdateHandler : IDisposable
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly ISquirrelApp _squirrel;
        private readonly IStateHandler _stateHandler;
        private TimerWithElapsedCancellationAsync _timer;

        public SelfUpdateHandler(ISquirrelApp squirrel, IDbContextFactory contextFactory, IStateHandler stateHandler) {
            _squirrel = squirrel;
            _contextFactory = contextFactory;
            _stateHandler = stateHandler;
        }

        public void Dispose() {
            _timer?.Dispose();
        }

        public Task HandleUpdateState() => Task.Run(async () => {
            await OnElapsed().ConfigureAwait(false);
            _timer = new TimerWithElapsedCancellationAsync(TimeSpan.FromHours(1).TotalMilliseconds, OnElapsed);
        });

        private async Task<bool> OnElapsed() {
            try {
                var version = await _squirrel.GetNewVersion().ConfigureAwait(false);
                if (version != null) {
                    using (_contextFactory.Create())
                        await _stateHandler.UpdateAvailable(version).ConfigureAwait(false);
                    // TODO: We should save?
                    return false;
                }
            } catch (Exception ex) {
                MainLog.Logger.Warn(ex.Format());
            }
            return true;
        }
    }
}