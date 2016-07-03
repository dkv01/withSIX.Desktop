// <copyright company="SIX Networks GmbH" file="AppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Akavache;
using Akavache.Sqlite3.Internal;
using AutoMapper;
using Caliburn.Micro;
using NDepend.Path;
using ReactiveUI;
using ShortBus;
using ShortBus.SimpleInjector;
using SimpleInjector;
using SmartAssembly.ReportException;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.ContentEngine.Infra.Services;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.MVVM;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Infra.Cache;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Core.Presentation.Decorators;
using SN.withSIX.Core.Presentation.Extensions;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Core.Presentation.Wpf;
using SN.withSIX.Core.Presentation.Wpf.Extensions;
using SN.withSIX.Core.Presentation.Wpf.Services;
using SN.withSIX.Core.Services;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Factories;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.MVVM.Usecases;
using SN.withSIX.Mini.Applications.MVVM.ViewModels;
using SN.withSIX.Mini.Applications.NotificationHandlers;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Infra.Api;
using SN.withSIX.Mini.Infra.Data.Services;
using SN.withSIX.Mini.Presentation.Core;
using SN.withSIX.Mini.Presentation.Core.Commands;
using SN.withSIX.Mini.Presentation.Core.Services;
using SN.withSIX.Mini.Presentation.Electron;
using SN.withSIX.Mini.Presentation.Wpf.Services;
using SN.withSIX.Sync.Core;
using SN.withSIX.Sync.Core.Legacy;
using SN.withSIX.Sync.Core.Packages;
using SN.withSIX.Sync.Core.Transfer;
using SN.withSIX.Sync.Core.Transfer.MirrorSelectors;
using SN.withSIX.Sync.Core.Transfer.Protocols;
using SN.withSIX.Sync.Core.Transfer.Protocols.Handlers;
using Splat;
using Action = System.Action;
using DefaultViewLocator = SN.withSIX.Mini.Presentation.Wpf.Services.DefaultViewLocator;
using IDependencyResolver = ShortBus.IDependencyResolver;
using IScreen = ReactiveUI.IScreen;
using ViewLocator = Caliburn.Micro.ViewLocator;

namespace SN.withSIX.Mini.Presentation.Wpf
{
    class WpfAppBootstrapper : AppBootstrapper
    {
        public WpfAppBootstrapper(Container container, IMutableDependencyResolver dependencyResolver)
            : base(container, dependencyResolver) {}

        protected override void InitializeCM() {
            base.InitializeCM();
            // Legacy
            Initialize(); // initialize CM framework
        }
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

    class AppBootstrapper : BootstrapperBase, IDisposable
    {
        static readonly IAbsoluteDirectoryPath assemblyPath =
            CommonBase.AssemblyLoader.GetNetEntryPath();
        // = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).ToAbsoluteDirectoryPath();
        static readonly Assembly[] coreAssemblies = new[] {
            typeof (Game).Assembly, typeof (IDomainService).Assembly,
            typeof (Tools).Assembly, typeof (Package).Assembly,
            typeof (IContentEngine).Assembly
        }.Distinct().ToArray();
        static readonly Assembly[] infraAssemblies = new[] {
            typeof (AutoMapperInfraApiConfig).Assembly, typeof (GameContext).Assembly,
            typeof (ImageCacheManager).Assembly,
            typeof (IContentEngineGameContext).Assembly
        }.Distinct().ToArray();
        static readonly Assembly[] presentationAssemblies = new[] {
            typeof (App).Assembly, typeof(NodeApi).Assembly, typeof(ApiPortHandler).Assembly, typeof (SingleInstanceApp).Assembly,
            typeof (IPresentationService).Assembly
        }.Distinct().ToArray();
        static readonly Assembly[] applicationAssemblies = new[] {
            typeof (GameSettingsApiModel).Assembly,
            typeof (IWpfStartupManager).Assembly, typeof (IDialogManager).Assembly,
            typeof(GetMiniMain).Assembly
        }.Distinct().ToArray();
        static readonly Assembly[] pluginAssemblies = DiscoverAndLoadPlugins().Distinct().ToArray();
        private static readonly Assembly[] platformAssemblies = DiscoverAndLoadPlatform().Distinct().ToArray();
        readonly Container _container;
        readonly IMutableDependencyResolver _dependencyResolver;
        readonly Paths _paths;
        TaskPoolScheduler _cacheScheduler;
        IEnumerable<IInitializer> _initializers;
        private Func<bool> _isPremium;
        IMiniMainWindowViewModel _mainVm;
        private IDisposable _stateSetter;

        internal AppBootstrapper(Container container, IMutableDependencyResolver dependencyResolver) : base(true) {
            _container = container;
            _dependencyResolver = dependencyResolver;
            _paths = new Paths();

            Common.Paths.SetPaths(null, _paths.RoamingDataPath, _paths.LocalDataPath, toolPath: _paths.ToolPath);
            InitializeCM();
            if (!Common.IsDebugEnabled)
                ErrorHandler.Report = Report;

            RxApp.SupportsRangeNotifications = false;
            SetupPaths();
            SetupRx();
            SetupCM();
            SetupContainer();
        }

        private BackgroundTasks BackgroundTasks { get; } = new BackgroundTasks();

        public void Dispose() {
            if (!Entrypoint.CommandMode)
                End().WaitSpecial();
            _stateSetter?.Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Report(Exception ex) {
            ExceptionReporting.Report(ex);
        }

        private void ConfigureInstances() {
            CoreCheat.SetServices(_container.GetInstance<ICoreCheatImpl>());
            Cheat.SetServices(_container.GetInstance<ICheatImpl>());
            Cache.ImageFiles = _container.GetInstance<Cache.IImageFileCache>();
            Tools.RegisterServices(_container.GetInstance<ToolsServices>());
            SyncEvilGlobal.Setup(_container.GetInstance<EvilGlobalServices>(), () => _isPremium() ? 6 : 3);
            UiRoot.Main = new UiRoot(_container.GetInstance<IDialogManager>(),
                _container.GetInstance<ISpecialDialogManager>());
            _initializers = _container.GetAllInstances<IInitializer>();
        }

        protected virtual void InitializeCM() {
            // Noop;
        }

        static IEnumerable<Assembly> DiscoverAndLoadPlatform() => DiscoverPlatformDlls()
            .Select(Assembly.LoadFrom);

        static IEnumerable<string> DiscoverPlatformDlls() {
            var win8Version = new Version(6, 2, 9200, 0);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= win8Version) {
                // its win8 or higher.
                yield return assemblyPath.GetChildFileWithName("SN.withSIX.Core.Presentation.WinRT.dll").ToString();
            }
        }

        static IEnumerable<Assembly> DiscoverAndLoadPlugins() => DiscoverPluginDlls()
            .Select(Assembly.LoadFrom);

        static IEnumerable<string> DiscoverPluginDlls()
            => assemblyPath.DirectoryInfo.GetFiles("*.plugin.*.dll").Select(x => x.FullName);

        void SetupPaths() {
            Directory.CreateDirectory(_paths.LocalDataPath.ToString());
            Directory.CreateDirectory(_paths.RoamingDataPath.ToString());
            StartupManager.Setup7z(_paths.AppPath);
        }

        void SetupRxUiDepResolver() {
            var dependencyResolver = new SimpleInjectorMutableDependencyResolver(_container, Locator.CurrentMutable);
            Locator.CurrentMutable = dependencyResolver;
            Locator.Current = dependencyResolver;
            dependencyResolver.InitializeSplat();
            dependencyResolver.InitializeReactiveUI();
        }

        public async Task Startup(Func<Task> act) {
            try {
                var dbContextFactory = _container.GetInstance<IDbContextFactory>();
                using (var scope = dbContextFactory.Create()) {
                    await StartupInternal().ConfigureAwait(false);
                    // We dont want the UI action to inherit our ctx..
                    using (dbContextFactory.SuppressAmbientContext())
                        await act().ConfigureAwait(false);
                    await AfterWindow().ConfigureAwait(false);
                    await scope.SaveChangesAsync().ConfigureAwait(false);
                }
                BackgroundActions();
                await HandleWaitForBackgroundTasks().ConfigureAwait(false);
                Cheat.SetInitialized();
                await
                    Task.Factory.StartNew(
                        () => new SIHandler().HandleSingleInstanceCall(Environment.GetCommandLineArgs().Skip(1).ToList()))
                        .ConfigureAwait(false);
            } catch (SQLiteException ex) {
                var message =
                    $"There appears to be a problem with your database. If the problem persists, you can delete the databases from:\n{Common.Paths.LocalDataPath} and {Common.Paths.DataPath}" +
                    "\nError message: " + ex.Message;
                await
                    _container.GetInstance<IDialogManager>()
                        .MessageBox(new MessageBoxDialogParams(message, "Sync: A problem was found in your database"))
                        .ConfigureAwait(false);
                throw;
            }
        }

        private async Task HandleWaitForBackgroundTasks() {
            var checkedFile = _paths.AppPath.GetChildFileWithName("checked.txt");
            if (!checkedFile.Exists) {
                await BackgroundTasks.Await().ConfigureAwait(false);
                File.WriteAllText(checkedFile.ToString(), DateTime.UtcNow.ToString());
            }
        }

        private async Task StartupInternal() {
            ConfigureInstances();

            var settingsStorage = _container.GetInstance<ISettingsStorage>();
            var settings = await settingsStorage.GetSettings().ConfigureAwait(false);
            await SetupApiPort(settings, settingsStorage).ConfigureAwait(false);
            TryHandleFirefoxInBackground();
            //TryInstallFlashInBackground();
            TryHandlePorts();
            _isPremium = () => (settings.Secure.Login?.IsPremium).GetValueOrDefault();
            if (settings.Local.EnableDiagnosticsMode)
                Common.Flags.Verbose = true;
            MappingExtensions.Mapper = new MapperConfiguration(cfg => {
                cfg.SetupConverters();
                foreach (var i in _initializers.OfType<IAMInitializer>())
                    i.ConfigureAutoMapper(cfg);
            }).CreateMapper();
            await RunInitializers().ConfigureAwait(false);
        }

        private static async Task SetupApiPort(Settings settings, ISettingsStorage settingsStorage) {
            if (Cheat.Args.Port.HasValue && settings.Local.ApiPort != Cheat.Args.Port) {
                settings.Local.ApiPort = Cheat.Args.Port;
                await settingsStorage.SaveChanges().ConfigureAwait(false);
            }
            Consts.ApiPort = settings.ApiPort;
        }

        private void TryInstallFlashInBackground() => Task.Factory.StartNew(InstallFlash,
            TaskCreationOptions.LongRunning).Unwrap();

        private void TryHandleFirefoxInBackground()
            => BackgroundTasks.RegisterTask(Task.Factory.StartNew(TryHandleFirefox,
                TaskCreationOptions.LongRunning));

        private static Task InstallFlash() => new FlashHandler(CommonUrls.FlashUri).InstallFlash();

        private void TryHandlePorts() {
            try {
                HandleSystem();
            } catch (OperationCanceledException ex) {
                MainLog.Logger.FormattedFatalException(ex, "Failure setting up API ports");
                _container.GetInstance<IDialogManager>().MessageBox(
                    new MessageBoxDialogParams(
                        "Configuration of API ports are required but were cancelled by the user.\nPlease allow the Elevation prompt on retry. The application is now closing.",
                        "Sync: API Ports Configuration cancelled"));
                Environment.Exit(1);
            }
        }

        private void TryHandleFirefox() {
            try {
                ApiPortHandler.SetupFirefox(_container.GetInstance<IProcessManager>());
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Failure processing firefox");
                Console.WriteLine("Failure processing firefox " + ex);
                //throw; // TODO 
            }
        }

        private void HandleSystem() {
            var pm = _container.GetInstance<IProcessManager>();
            var si = Infra.Api.Initializer.BuildSi(pm);
            if ((Consts.HttpAddress == null || si.IsHttpPortRegistered) &&
                (Consts.HttpsAddress == null || si.IsSslRegistered()))
                return;
            ApiPortHandler.SetupApiPort(Consts.HttpAddress, Consts.HttpsAddress, pm);
            si = Infra.Api.Initializer.BuildSi(pm); // to output
        }

        async Task HandlemainVm() => _mainVm = await new GetMiniMain().ExecuteWrapped().ConfigureAwait(false);

        async Task RunInitializers() {
            foreach (var i in _initializers)
                await i.Initialize().ConfigureAwait(false);

            // Because we don't guarantee initializers order atm..
            // TODO: Handle this differently??
            await PostInitialize().ConfigureAwait(false);
        }

        async Task PostInitialize() {
            await _container.GetInstance<ISetupGameStuff>().Initialize().ConfigureAwait(false);
            await _container.GetInstance<IStateHandler>().Initialize().ConfigureAwait(false);
            if (!Cheat.IsNode)
                await HandlemainVm().ConfigureAwait(false);
        }

        private void InstallToolsInBackground() => Task.Factory.StartNew(InstallTools, TaskCreationOptions.LongRunning);

        private async Task InstallTools() {
            try {
                await _container.GetInstance<IToolsCheat>().SingleToolsInstallTask().ConfigureAwait(false);
            } catch (Exception ex) {
                MainLog.Logger.FormattedErrorException(ex, "Error while installing tools");
                throw;
            }
        }

        private Task SetState(ActionTabState x)
            =>
                Entrypoint.Api.SetState(x.Type == ActionType.Start ? BusyState.On : BusyState.Off,
                    x.ChildAction.Details ?? x.Text, x.Progress);


        protected static string GetHumanReadableActionName(string action) => action.Split('.').Last();

        internal IMiniMainWindowViewModel GetMainWindowViewModel() => _container.GetInstance<IMiniMainWindowViewModel>();

        void SetupContainer() {
            ConfigureContainer();
            RegisterServices();
            RegisterViews();

            if (Entrypoint.CommandMode)
                _container.RegisterPlugins<BaseCommand>(presentationAssemblies);
            // Fix JsonSerializer..
            //Locator.CurrentMutable.Register(() => GameContextJsonImplementation.Settings, typeof(JsonSerializerSettings));
        }

        void SetupRx() {
            //SetupRxUiDepResolver();
            var viewLocator = new DefaultViewLocator();
            // If we use the withSIX.Core.Presentation.Wpf.Services. one then we get Reactivecommands as text etc..
            //var jsonSerializerSettings = new JsonSerializerSettings() { DateTimeZoneHandling = DateTimeZoneHandling.Utc };
            _dependencyResolver.Register(() => viewLocator, typeof (IViewLocator));
            //_dependencyResolver.Register(() => jsonSerializerSettings, typeof (JsonSerializerSettings));
        }

        [Obsolete("TODO: Remove CM ViewLocation with RXUI everywhere")]
        void SetupCM() {
            var original = ViewLocator.LocateForModel;
            ViewLocator.LocateForModel = (o, dependencyObject, arg3) => {
                var v = original(o, dependencyObject, arg3);
                // TODO: Lacks CM's Context/target support
                if (v == null || v is TextBlock) {
                    var rxv = (UIElement) ReactiveUI.ViewLocator.Current.ResolveView(o);
                    if (rxv != null)
                        v = rxv;
                }

                var vFor = v as IViewFor;
                if (vFor != null)
                    vFor.ViewModel = o;

                return v;
            };
            //ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Presentation.Wpf.Views", "SN.withSIX.Core.Presentation.Wpf.Views");
            ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Applications.MVVM.ViewModels.Popups",
                "SN.withSIX.Core.Presentation.Wpf.Views.Popups");
            ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Applications.MVVM.ViewModels.Dialogs",
                "SN.withSIX.Core.Presentation.Wpf.Views.Dialogs");
            ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Applications.MVVM.ViewModels",
                "SN.withSIX.Core.Presentation.Wpf.Views");
        }

        void ConfigureContainer() {
            // TODO: Disable this once we could disable registering inherited interfaces??
            _container.Options.LifestyleSelectionBehavior = new CustomLifestyleSelectionBehavior();
            _container.Options.AllowOverridingRegistrations = true;
            _container.AllowResolvingFuncFactories();
            _container.AllowResolvingLazyFactories();
        }

        void RegisterCaches() {
            _cacheScheduler = TaskPoolScheduler.Default;

            _container.RegisterInitializer<IBlobCache>(RegisterCache);
            RegisterMemoryCaches();
            RegisterLocalCaches();
            RegisterRoamingCaches();
        }

        void RegisterMemoryCaches() {
            _container.RegisterSingleton<IInMemoryCache>(() => new InMemoryCache(_cacheScheduler));
        }

        void RegisterLocalCaches() {
            _container.RegisterSingleton<ILocalCache>(
                () => new LocalCache(_paths.LocalDataPath.GetChildFileWithName("cache.db").ToString(),
                    _cacheScheduler));
            _container.RegisterSingleton<IImageCache>(
                () =>
                    new ImageCache(_paths.LocalDataPath.GetChildFileWithName("image.cache.db").ToString(),
                        _cacheScheduler));
            _container.RegisterSingleton<IApiLocalCache>(
                () =>
                    new ApiLocalCache(
                        _paths.LocalDataPath.GetChildFileWithName("api.cache.db").ToString(), _cacheScheduler));
        }

        void RegisterRoamingCaches() {
            _container.RegisterSingleton<IUserCache>(
                () => new UserCache(_paths.RoamingDataPath.GetChildFileWithName("cache.db").ToString(),
                    _cacheScheduler));
            _container.RegisterSingleton<ISecureCache>(
                () =>
                    new SecureCache(_paths.RoamingDataPath.GetChildFileWithName("secure-cache.db").ToString(),
                        new EncryptionProvider(), _cacheScheduler));
        }

        void RegisterCache<T>(T cache) where T : IBlobCache {
            var cacheManager = _container.GetInstance<ICacheManager>();
            cacheManager.RegisterCache(cache);
        }

        void RegisterViews() {
            var viewInterfaceFilterType = typeof (IViewFor);
            _dependencyResolver.RegisterAllInterfaces<IViewFor>(GetPresentationAssemblies(),
                (type, type1) => viewInterfaceFilterType.IsAssignableFrom(type));
            //dependencyResolver.RegisterConstant(this, typeof (IScreen));

            // TODO: We might still want to leverage S.I also for RXUI View resolution, so that Views may import (presentation) services?
            _container.RegisterSingleton(() => _mainVm);
            _container.RegisterSingleton<IScreen>(_container.GetInstance<IMiniMainWindowViewModel>);

            //_container.RegisterPlugins<ISettingsTabViewModel>(applicationAssemblies);

            /*
            _container.RegisterAllInterfaces<IViewFor>(GetPresentationAssemblies(), (type, type1) => viewInterfaceFilterType.IsAssignableFrom(type));
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
        }

        static IEnumerable<Assembly> GetPresentationAssemblies()
            => pluginAssemblies.Concat(platformAssemblies).Concat(presentationAssemblies);

        void RegisterServices() {
            _container.RegisterSingleton(() => Entrypoint.Api);

            RegisterRegisteredServices();

            _container.RegisterPlugins<INotificationProvider>(GetPresentationAssemblies(), Lifestyle.Singleton);
            //_container.RegisterSingleton<IDomainEventHandlerGrabber, DomainEventHandlerGrabber>();

            //_container.Register<IDepResolver, DepResolver>();
            var assemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => !x.FullName.Contains("edge") && x.FullName.Contains("SN.withSIX"))
                    .Concat(new[] {typeof (App).Assembly}).Distinct();
            _container.RegisterPlugins<IInitializer>(assemblies);
            // , Lifestyle.Singleton // fails
            _container.RegisterSingleton<IToolsInstaller>(
                () =>
                    new ToolsInstaller(_container.GetInstance<IFileDownloader>(),
                        new Restarter(_container.GetInstance<IShutdownHandler>(),
                            _container.GetInstance<IDialogManager>()),
                        _paths.ToolPath));

            _container.RegisterSingleton<IINstallerSessionFactory>(
                () =>
                    new InstallerSessionFactory(() => _isPremium(), _container.GetInstance<IToolsCheat>(),
                        _container.GetInstance<IContentEngine>(), _container.GetInstance<IAuthProvider>()));
            _container.RegisterSingleton<IContentInstallationService>(
                () =>
                    new ContentInstaller(evt => evt.Raise(), _container.GetInstance<IGameLocker>(),
                        _container.GetInstance<IINstallerSessionFactory>()));

            // LEGACY
            AppBootstrapperBase.ContainerConfiguration.RegisterEventAggregator(_container);
            _container.RegisterSingleton<IPathConfiguration>(() => Common.Paths);
            _container.RegisterSingleton(() => new ExportFactory<IWebClient>(OnExportLifetimeContextCreator));
            _container.RegisterSingleton<IShutdownHandler, WpfShutdownHandler>();
            if (Cheat.IsNode)
                _container.RegisterSingleton<IDialogManager, NodeDialogManager>();
            else
                _container.RegisterSingleton<IDialogManager, WpfDialogManager>();
            _container.RegisterSingleton<IWindowManager, CustomWindowManager>();

            _container.RegisterInitializer<IProcessManager>(ConfigureProcessManager);

            RegisterMediator();
            RegisterMessageBus();
            RegisterDownloader();
            RegisterTools();
            RegisterCaches();
        }

        void RegisterMessageBus() {
            _container.RegisterSingleton(new MessageBus());
            _container.RegisterSingleton<IMessageBus>(_container.GetInstance<MessageBus>);
        }

        void RegisterRegisteredServices() {
            _container.RegisterSingleAllInterfaces<IDomainService>(pluginAssemblies.Concat(coreAssemblies));
            _container.RegisterSingleAllInterfaces<IApplicationService>(pluginAssemblies.Concat(applicationAssemblies));
            _container.RegisterSingleAllInterfaces<IInfrastructureService>(pluginAssemblies.Concat(infraAssemblies));
            _container.RegisterSingleAllInterfaces<IPresentationService>(GetPresentationAssemblies());
        }

        void RegisterMediator() {
            _container.RegisterSingleton<IDependencyResolver, SimpleInjectorDependencyResolver>();
            _container.RegisterSingleton<IMediator, Mediator>();

            RegisterRequestHandlers(typeof (IAsyncRequestHandler<,>),
                typeof (IRequestHandler<,>));
            RegisterNotificationHandlers(typeof (INotificationHandler<>),
                typeof (IAsyncNotificationHandler<>));

            _container.RegisterDecorator<IMediator, ActionNotifierDecorator>(Lifestyle.Singleton);
            _container.RegisterDecorator<IMediator, GameWriteLockDecorator>(Lifestyle.Singleton);
            _container.RegisterDecorator<IMediator, DbScopeDecorator>(Lifestyle.Singleton);

            AppBootstrapperBase.ContainerConfiguration.RegisterMediatorDecorators(_container);

            // We now handle this over the WriteLock decorator instead..
            /*
            //var t = typeof (INeedGameContents);
            _container.RegisterDecorator(typeof (IRequestHandler<,>),
                typeof (RequestHandlerRequireGameContentDecorator<,>),
                Lifestyle.Singleton); //  context => t.IsAssignableFrom(context.ServiceType)
            _container.RegisterDecorator(typeof (IAsyncRequestHandler<,>),
                typeof (AsyncRequestHandlerRequireGameContentDecorator<,>), Lifestyle.Singleton); // context => t.IsAssignableFrom(context.ServiceType)
            */

            //_container.RegisterDecorator(typeof (IRequestHandler<,>), typeof (RequestHandlerWriteLockDecorator<,>), Lifestyle.Singleton);
            //_container.RegisterDecorator(typeof (IAsyncRequestHandler<,>), typeof (AsyncRequestHandlerWriteLockDecorator<,>), Lifestyle.Singleton);

            // DOesnt help ;-D
            _container.RegisterDecorator<IMediator, RequestMemoryDecorator>(Lifestyle.Singleton);
        }

        static Tuple<IWebClient, Action> OnExportLifetimeContextCreator() {
            var wc = new WebClient();
            return new Tuple<IWebClient, Action>(wc, wc.Dispose);
        }

        void RegisterRequestHandlers(params Type[] types) {
            foreach (var h in types) {
                _container.Register(h, applicationAssemblies.Concat(infraAssemblies), Lifestyle.Singleton);
                // TODO: Infra should not contain use cases. It's only here because CE is using Mediator to construct services: Not what it is designed for!
            }
        }

        void RegisterNotificationHandlers(params Type[] types) {
            foreach (var h in types) {
                var assemblies = applicationAssemblies.Concat(infraAssemblies).ToArray();
                _container.RegisterCollection(h, assemblies);
                // TODO: Infra should not contain use cases. It's only here because CE is using Mediator to construct services: Not what it is designed for!
            }
        }

        void RegisterDownloader() {
            RegisterDownloaderFactories();

            _container.RegisterSingleton<IZsyncLauncher, ZsyncLauncher>();
            _container.RegisterSingleton<IRsyncLauncher, RsyncLauncher>();
            _container.RegisterSingleton<RsyncOutputParser>();
            _container.RegisterSingleton<ZsyncOutputParser>();
            _container.RegisterSingleton<ICopyFile, FileCopier>();

            _container.Register<IHostChecker, HostChecker>();
            _container.Register<IHostCheckerWithPing, HostCheckerWithPing>();

            _container.RegisterPlugins<IDownloadProtocol>(coreAssemblies,
                Lifestyle.Singleton);
            _container.RegisterPlugins<IUploadProtocol>(coreAssemblies,
                Lifestyle.Singleton);

            _container.RegisterSingleton<IHttpDownloadProtocol, HttpDownloadProtocol>();
            _container.RegisterSingleton<IFileDownloader, FileDownloader>();
            _container.RegisterSingleton<IFileUploader, FileUploader>();
            _container.RegisterSingleton<IStringDownloader, StringDownloader>();
            _container.RegisterSingleton<IDataDownloader, DataDownloader>();
            //_container.Register<IMultiMirrorFileDownloader, MultiMirrorFileDownloader>();
            //_container.Register<IMirrorSelector, ScoreMirrorSelector>(() => new ScoreMirrorSelector());
            _container.RegisterSingleton<IFileDownloadHelper, FileDownloadHelper>();
            _container.RegisterSingleton<IZsyncMake, ZsyncMake>();

            _container.RegisterDecorator<IFileDownloader, LoggingFileDownloaderDecorator>(Lifestyle.Singleton);
            _container.RegisterDecorator<IFileUploader, LoggingFileUploaderDecorator>(Lifestyle.Singleton);

            _container.RegisterSingleton<Func<HostCheckerType>>(() => HostCheckerType.WithPing);
            _container.RegisterSingleton<Func<ProtocolPreference>>(() => ProtocolPreference.Any);

            // TODO: Get the max concurrent download setting... based on premium etc
            //settings.Local.MaxConcurrentDownloads
            _container
                .RegisterSingleton<Func<IMultiMirrorFileDownloader, ExportLifetimeContext<IFileQueueDownloader>>>(
                    x => {
                        var downloader = new MultiThreadedFileQueueDownloader(SyncEvilGlobal.Limiter, x);
                        return new ExportLifetimeContext<IFileQueueDownloader>(downloader, TaskExt.NullAction);
                    });
        }

        void RegisterDownloaderFactories() {
            // TODO: this is a replacement for the simple factory classes we have, they can be removed later?
            _container.RegisterSingleton<Func<IMirrorSelector, ExportLifetimeContext<IMultiMirrorFileDownloader>>>(
                x => new ExportLifetimeContext<IMultiMirrorFileDownloader>(
                    new MultiMirrorFileDownloader(_container.GetInstance<IFileDownloader>(), x), TaskExt.NullAction));
            _container.RegisterSingleton<Func<IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>>>(
                x => {
                    var hostChecker =
                        _container.GetInstance<Func<ExportLifetimeContext<IHostChecker>>>().Invoke();
                    var selector = new ScoreMirrorSelector(hostChecker.Value, x);
                    return new ExportLifetimeContext<IMirrorSelector>(selector, () => {
                        selector.Dispose();
                        hostChecker.Dispose();
                    });
                });
            _container
                .RegisterSingleton<Func<int, IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>>>(
                    (limit, x) => {
                        var hostChecker =
                            _container.GetInstance<Func<ExportLifetimeContext<IHostChecker>>>().Invoke();
                        var selector = new ScoreMirrorSelector(limit, hostChecker.Value, x);
                        return new ExportLifetimeContext<IMirrorSelector>(selector, () => {
                            selector.Dispose();
                            hostChecker.Dispose();
                        });
                    });
            _container.RegisterSingleton<Func<ExportLifetimeContext<IHostChecker>>>(() => {
                var hostCheckerType = _container.GetInstance<Func<HostCheckerType>>();
                var hostChecker = hostCheckerType() == HostCheckerType.WithPing
                    ? _container.GetInstance<IHostCheckerWithPing>()
                    : _container.GetInstance<IHostChecker>();
                return new ExportLifetimeContext<IHostChecker>(hostChecker, TaskExt.NullAction);
            });
        }

        static void ConfigureProcessManager(IProcessManager processMan) {
            processMan.MonitorKilled.Subscribe(killed =>
                MainLog.Logger.Warn(
                    "::ProcessManager:: Monitor Killed: {1} {0} due to inactive for over {2}", killed.Item2,
                    killed.Item4,
                    killed.Item3));
#if DEBUG
            processMan.Launched.Subscribe(
                launched =>
                    MainLog.Logger.Debug("::ProcessManager:: Launched {0}. {1} {2} from {3}", launched.Item2,
                        launched.Item1.FileName,
                        launched.Item1.Arguments, launched.Item1.WorkingDirectory));
            processMan.Terminated.Subscribe(
                terminated =>
                    MainLog.Logger.Debug("::ProcessManager:: Terminated {0} ({1})", terminated.Item3,
                        terminated.Item2));
            processMan.MonitorStarted.Subscribe(
                monitorStarted =>
                    MainLog.Logger.Debug("::ProcessManager:: Monitor Started: {1} {0}", monitorStarted.Item2,
                        monitorStarted.Item3));
            processMan.MonitorStopped.Subscribe(
                monitorStopped =>
                    MainLog.Logger.Debug("::ProcessManager:: Monitor Stopped: {1} {0}", monitorStopped.Item2,
                        monitorStopped.Item3));
#endif
        }

        void RegisterTools() {
            // TODO
            _container.RegisterSingleton(() => new Lazy<IWCFClient>());
            _container.RegisterSingleton(() => Tools.Generic);
            _container.RegisterSingleton(() => Tools.FileUtil);
            _container.RegisterSingleton<Tools.FileTools.IFileOps>(() => Tools.FileUtil.Ops);
            _container.RegisterSingleton(() => Tools.Compression);
            _container.RegisterSingleton(() => Tools.Compression.Gzip);
            _container.RegisterSingleton(() => Tools.HashEncryption);
            _container.RegisterSingleton(() => Tools.Processes);
            _container.RegisterSingleton(() => Tools.Processes.Uac);
            _container.RegisterSingleton(() => Tools.Serialization);
            _container.RegisterSingleton(() => Tools.Serialization.Json);
            _container.RegisterSingleton(() => Tools.Serialization.Xml);
            _container.RegisterSingleton(() => Tools.Serialization.Yaml);
            _container.RegisterSingleton(() => Tools.Transfer);
        }

        Task End() => Task.Factory.StartNew(EndInternal, TaskCreationOptions.LongRunning).Unwrap();

        private async Task EndInternal() {
            // This creates an ambient context when shutdown is called from a usecase.
            // so we suppress it
            var dbContextFactory = _container.GetInstance<IDbContextFactory>();
            using (dbContextFactory.SuppressAmbientContext())
            using (var scope = dbContextFactory.Create()) {
                await RunDeinitializers().ConfigureAwait(false);
                await scope.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        async Task RunDeinitializers() {
            foreach (var i in _initializers)
                await i.Deinitialize().ConfigureAwait(false);
        }

        public CommandRunner GetCommandMode() => _container.GetInstance<CommandRunner>();

        private async Task AfterWindow() {
            if (ErrorHandler.Handler != null)
                UserError.RegisterHandler(error => ErrorHandler.Handler.Handler(error));

            var settings =
                await
                    _container.GetInstance<IDbContextLocator>()
                        .GetSettingsContext()
                        .GetSettings()
                        .ConfigureAwait(false);
            new StartWithWindowsHandler().HandleStartWithWindows(settings.Local.StartWithWindows);

            foreach (var i in _initializers.OfType<IInitializeAfterUI>())
              await i.InitializeAfterUI().ConfigureAwait(false);
        }

        private void BackgroundActions() {
            InstallToolsInBackground();
            HandleImportInBackground();
            if (!Cheat.IsNode)
                HandleUpdateStateInBackground();
            else {
                var stateHandler = _container.GetInstance<IStateHandler>();
                _stateSetter = stateHandler.StatusObservable.ObserveOn(ThreadPoolScheduler.Instance)
                    .Throttle(TimeSpan.FromMilliseconds(500))
                    .ConcatTask(SetState)
                    .Subscribe();
            }
        }

        private void HandleUpdateStateInBackground()
            =>
                Task.Factory.StartNew(_container.GetInstance<SelfUpdateHandler>().HandleUpdateState,
                    TaskCreationOptions.LongRunning);

        // TODO: Should only import after we have synchronized collection content etc??
        // Or we create LocalContent as temporary placeholder, and then in the future we should look at converting to network content then??
        void HandleImportInBackground() => TryHandleImport();

        private async Task TryHandleImport() {
            try {
                await Task.Factory.StartNew(HandleImportInternal, TaskCreationOptions.LongRunning).ConfigureAwait(false);
            } catch (Exception ex) {
                await UserError.Throw(ex.Message, ex);
            }
        }

        async Task HandleImportInternal() {
            var importer = _container.GetInstance<IPlayWithSixImporter>();
            var dialogManager = _container.GetInstance<IDialogManager>();
            using (var scope = _container.GetInstance<IDbContextFactory>().Create()) {
                var settingsFile = importer.DetectPwSSettings();
                if (settingsFile == null)
                    return;
                if (!await importer.ShouldImport().ConfigureAwait(false))
                    return;
                var result =
                    await
                        dialogManager
                            .MessageBox(
                                new MessageBoxDialogParams(
                                    "Import existing PwS Settings? (You can also do this later from the Settings)",
                                    "Import?", SixMessageBoxButton.YesNo))
                            .ConfigureAwait(false);
                if (!result.IsYes()) {
                    var ctx = _container.GetInstance<IDbContextLocator>().GetSettingsContext();
                    var settings = await ctx.GetSettings().ConfigureAwait(false);
                    settings.Local.DeclinedPlaywithSixImport = true;
                    await scope.SaveChangesAsync().ConfigureAwait(false);
                    return;
                }
                await importer.ImportPwsSettings(settingsFile).ConfigureAwait(false);
                await
                    dialogManager.MessageBox(new MessageBoxDialogParams("Settings imported succesfully", "Success"))
                        .ConfigureAwait(false);
                await scope.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        class Paths
        {
            static IAbsoluteDirectoryPath LocalDataRootPath { get; } = PathConfiguration.GetLocalDataRootPath();
            static IAbsoluteDirectoryPath RoamingDataRootPath { get; } = PathConfiguration.GetRoamingRootPath();
            static IAbsoluteDirectoryPath SharedLocalDataRootPath { get; } =
                LocalDataRootPath.GetChildDirectoryWithName("Shared");
            public IAbsoluteDirectoryPath LocalDataPath { get; } =
                LocalDataRootPath.GetChildDirectoryWithName(Consts.DirectoryTitle);
            public IAbsoluteDirectoryPath RoamingDataPath { get; } =
                RoamingDataRootPath.GetChildDirectoryWithName(Consts.DirectoryTitle);
            public IAbsoluteDirectoryPath AppPath { get; } = CommonBase.AssemblyLoader.GetNetEntryPath();
            public IAbsoluteDirectoryPath ToolPath { get; } = SharedLocalDataRootPath.GetChildDirectoryWithName("Tools")
                ;
        }
    }
}