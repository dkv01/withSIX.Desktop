// <copyright company="SIX Networks GmbH" file="AppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Akavache.Sqlite3.Internal;
using AutoMapper;
using MediatR;
using NDepend.Path;
using SimpleInjector;
using withSIX.Api.Models.Extensions;
using withSIX.ContentEngine.Core;
using withSIX.ContentEngine.Infra.Services;
using withSIX.Core;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Cache;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation;
using withSIX.Core.Presentation.Decorators;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.NotificationHandlers;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Mini.Infra.Data.Services;
using withSIX.Mini.Presentation.Core.Commands;
using withSIX.Mini.Presentation.Core.Services;
using withSIX.Steam.Core;
using withSIX.Steam.Infra;
using withSIX.Sync.Core;
using withSIX.Sync.Core.Legacy;
using withSIX.Sync.Core.Packages;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.MirrorSelectors;
using withSIX.Sync.Core.Transfer.Protocols;
using withSIX.Sync.Core.Transfer.Protocols.Handlers;

namespace withSIX.Mini.Presentation.Core
{
    public abstract class AppBootstrapper : IDisposable
    {
        public IAbsoluteDirectoryPath RootPath { get; set; }
        static readonly IAbsoluteDirectoryPath assemblyPath =
            CommonBase.AssemblyLoader.GetNetEntryPath();
        // = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).ToAbsoluteDirectoryPath();
        static readonly Assembly[] coreAssemblies = new[] {
            typeof(Game).GetTypeInfo().Assembly, typeof(IDomainService).GetTypeInfo().Assembly,
            typeof(Tools).GetTypeInfo().Assembly, typeof(Package).GetTypeInfo().Assembly,
            typeof(IContentEngine).GetTypeInfo().Assembly
        }.Distinct().ToArray();
        static readonly Assembly[] infraAssemblies = new[] {
            typeof(SteamServiceSession).GetTypeInfo().Assembly,
            typeof(GameContext).GetTypeInfo().Assembly,
            typeof(ImageCacheManager).GetTypeInfo().Assembly,
            typeof(IContentEngineGameContext).GetTypeInfo().Assembly
        }.Distinct().ToArray();
        static readonly Assembly[] globalPresentationAssemblies = new[] {
            typeof(AppBootstrapper).GetTypeInfo().Assembly,
            typeof(IPresentationService).GetTypeInfo().Assembly
        }.Distinct().ToArray();
        static readonly Assembly[] globalApplicationAssemblies = new[] {
            typeof(GameSettingsApiModel).GetTypeInfo().Assembly,
            typeof(IDialogManager).GetTypeInfo().Assembly
        }.Distinct().ToArray();
        private Assembly[] pluginAssemblies;
        private Assembly[] platformAssemblies;
        private Assembly[] _applicationAssemblies;
        private readonly string[] _args;
        Paths _paths;
        private Assembly[] _presentationAssemblies;
        protected readonly Container Container;
        TaskPoolScheduler _cacheScheduler;
        IEnumerable<IInitializer> _initializers;
        private Func<bool> _isPremium;


        public virtual void Configure() {
            pluginAssemblies = DiscoverAndLoadPlugins().Distinct().ToArray();
            platformAssemblies = DiscoverAndLoadPlatform().Distinct().ToArray();

            CommandMode = DetermineCommandMode();

            _applicationAssemblies = GetApplicationAssemblies().ToArray();
            _presentationAssemblies = GetPresentationAssemblies().ToArray();

            LowInitializer();

            _paths = new Paths();
            Common.Paths = new PathConfiguration();
            Common.Paths.SetPaths(null, _paths.RoamingDataPath, _paths.LocalDataPath, toolPath: _paths.ToolPath);

            SetupPaths();
            SetupContainer();
            UserErrorHandling.Setup();
        }

        protected virtual IEnumerable<Assembly> GetInfraAssemblies
            =>
            new[] {AssemblyLoadFrom(RootPath.GetChildFileWithName("withSIX.Mini.Presentation.Owin.Core.dll"))}.Concat(
                infraAssemblies);

        protected AppBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath;
            _args = args;
            Container = new Container();
        }

        protected abstract void LowInitializer();

        public bool CommandMode { get; set; }

        private BackgroundTasks BackgroundTasks { get; } = new BackgroundTasks();

        public void Dispose() {
            Dispose(true);
        }

        protected virtual IEnumerable<Assembly> GetApplicationAssemblies() => globalApplicationAssemblies;

        protected virtual void Dispose(bool d) {
            if (!CommandMode)
                EndOv();
            Container.Dispose();
        }

        protected virtual void EndOv() => End().WaitAndUnwrapException();

        protected virtual void ConfigureInstances() {
            Game.SteamHelper = SteamHelper.Create(); // TODO: Move
            var bridge = Container.GetInstance<IBridge>();
            GameContextJsonImplementation.Settings = bridge.GameContextSettings();
            DataCheat.Instance = Container.GetInstance<ICallContextService>();
            CoreCheat.SetServices(Container.GetInstance<ICoreCheatImpl>());
            Cheat.SetServices(Container.GetInstance<ICheatImpl>());
            RegisterToolServices();
            SyncEvilGlobal.Setup(Container.GetInstance<EvilGlobalServices>(), () => _isPremium() ? 6 : 3);
            _initializers = Container.GetAllInstances<IInitializer>();
        }

        private void RegisterToolServices() {
            Tools.RegisterServices(Container.GetInstance<ToolsServices>());
            Tools.FileTools.FileOps.ShouldRetry = async (s, s1, e) => {
                var result =
                    await
                        UserErrorHandler.HandleUserError(new UserErrorModel(s, s1, RecoveryCommands.YesNoCommands, null,
                            e));
                return result == RecoveryOptionResultModel.RetryOperation;
            };
            Tools.InformUserError = (s, s1, e) => UserErrorHandler.HandleUserError(new InformationalUserError(e, s1, s));
        }

        IEnumerable<Assembly> DiscoverAndLoadPlatform() => DiscoverPlatformDlls()
            .Select(AssemblyLoadFrom);

        static IEnumerable<string> DiscoverPlatformDlls() {
            return Enumerable.Empty<string>();
            /*
            var win8Version = new Version(6, 2, 9200, 0);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= win8Version) {
                // its win8 or higher.
                yield return assemblyPath.GetChildFileWithName("withSIX.Core.Presentation.WinRT.dll").ToString();
            }*/
        }

        IEnumerable<Assembly> DiscoverAndLoadPlugins() => DiscoverPluginDlls()
            .Select(AssemblyLoadFrom);

        protected Assembly AssemblyLoadFrom(IAbsoluteFilePath arg) => AssemblyLoadFrom(arg.ToString());

        protected abstract Assembly AssemblyLoadFrom(string arg);

        static IEnumerable<string> DiscoverPluginDlls()
            => assemblyPath.DirectoryInfo.GetFiles("*.mini.plugin.*.dll").Select(x => x.FullName);

        void SetupPaths() {
            Directory.CreateDirectory(_paths.LocalDataPath.ToString());
            Directory.CreateDirectory(_paths.RoamingDataPath.ToString());
        }

        bool DetermineCommandMode() {
            if (!_args.Any())
                return false;
            var firstArgument = _args.First();
            return !firstArgument.StartsWith("-") && !firstArgument.StartsWith("syncws://");
        }

        public async Task Startup(Func<Task> act) {
            if (CommandMode)
                RunCommands();
            else
                await StartupUiMode(act).ConfigureAwait(false);
        }

        private async Task StartupUiMode(Func<Task> act) {
            try {
                ConfigureInstances();
                var dbContextFactory = Container.GetInstance<IDbContextFactory>();
                using (var scope = dbContextFactory.Create()) {
                    await StartupInternal().ConfigureAwait(false);
                    // We dont want the UI action to inherit our ctx..
                    using (dbContextFactory.SuppressAmbientContext())
                        await act().ConfigureAwait(false);
                    await AfterUi().ConfigureAwait(false);
                    await scope.SaveChangesAsync().ConfigureAwait(false);
                }
                BackgroundActions();
                await HandleWaitForBackgroundTasks().ConfigureAwait(false);
                // TODO: call from node?
                var task = TaskExt.StartLongRunningTask(
                    () =>
                            new SIHandler().HandleSingleInstanceCall(Common.Flags.FullStartupParameters.ToList()));
            } catch (SQLiteException ex) {
                MainLog.Logger.FormattedErrorException(ex, "A problem was found with a database");
                var message =
                    $"There appears to be a problem with your database. If the problem persists, you can delete the databases from:\n{Common.Paths.LocalDataPath} and {Common.Paths.DataPath}" +
                    "\nError message: " + ex.Message;
                await
                    Container.GetInstance<IDialogManager>()
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
            var settingsStorage = Container.GetInstance<ISettingsStorage>();
            var settings = await settingsStorage.GetSettings().ConfigureAwait(false);
            await SetupApiPort(settings, settingsStorage).ConfigureAwait(false);
            TryHandleFirefoxInBackground();
            //TryInstallFlashInBackground();
            _isPremium = () => (settings.Secure.Login?.IsPremium).GetValueOrDefault();
            if (settings.Local.EnableDiagnosticsMode)
                Common.Flags.Verbose = true;
            MappingExtensions.Mapper = new MapperConfiguration(cfg => {
                cfg.SetupConverters();
                foreach (var p in Container.GetAllInstances<Profile>())
                    cfg.AddProfile(p);
            }).CreateMapper();

            await RunInitializers().ConfigureAwait(false);
        }

        private static async Task SetupApiPort(Settings settings, ISettingsStorage settingsStorage) {
            if (Cheat.Args.Port.HasValue && (settings.Local.ApiPort != Cheat.Args.Port)) {
                settings.Local.ApiPort = Cheat.Args.Port;
                await settingsStorage.SaveChanges().ConfigureAwait(false);
            }
            Consts.ApiPort = settings.ApiPort;
        }

        private void TryHandleFirefoxInBackground()
            => BackgroundTasks.RegisterTask(TaskExt.StartLongRunningTask(() => TryHandleFirefox()));


        // TODO: Move to windows specific
        private void TryHandleFirefox() {
            try {
                FirefoxHandler.SetupFirefox(Container.GetInstance<IProcessManager>());
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Failure processing firefox");
                Console.WriteLine("Failure processing firefox " + ex);
                //throw; // TODO 
            }
        }

        async Task RunInitializers() {
            foreach (var i in _initializers)
                await i.Initialize().ConfigureAwait(false);

            // Because we don't guarantee initializers order atm..
            // TODO: Handle this differently??
            await PostInitialize().ConfigureAwait(false);
        }

        protected virtual async Task PostInitialize() {
            await Container.GetInstance<ISetupGameStuff>().Initialize().ConfigureAwait(false);
            await Container.GetInstance<IStateHandler>().Initialize().ConfigureAwait(false);
        }

        private void InstallToolsInBackground() => TaskExt.StartLongRunningTask(InstallTools);

        private async Task InstallTools() {
            try {
                await Container.GetInstance<IToolsCheat>().SingleToolsInstallTask().ConfigureAwait(false);
            } catch (Exception ex) {
                MainLog.Logger.FormattedErrorException(ex, "Error while installing tools");
                throw;
            }
        }

        protected static string GetHumanReadableActionName(string action) => action.Split('.').Last();

        void SetupContainer() {
            ConfigureContainer();
            RegisterServices();
            RegisterViews();

            if (CommandMode)
                RegisterPlugins<BaseCommand>(_presentationAssemblies);

            var serviceReg = new ServiceRegisterer(Container);
            foreach (var t in GetTypes<ServiceRegistry>(pluginAssemblies))
                Activator.CreateInstance(t, serviceReg);
        }

        protected abstract IEnumerable<Type> GetTypes<T>(IEnumerable<Assembly> assemblies);

        protected abstract void RegisterPlugins<T>(IEnumerable<Assembly> assemblies, Lifestyle style = null) where T : class;

        protected abstract void ConfigureContainer();

        void RegisterCaches() {
            _cacheScheduler = TaskPoolScheduler.Default;

            Container.RegisterInitializer<IBlobCache>(RegisterCache);
            RegisterMemoryCaches();
            RegisterLocalCaches();
            RegisterRoamingCaches();
        }

        void RegisterMemoryCaches() {
            Container.RegisterSingleton<IInMemoryCache>(() => new InMemoryCache(_cacheScheduler));
        }

        void RegisterLocalCaches() {
            Container.RegisterSingleton<ILocalCache>(
                () => new LocalCache(_paths.LocalDataPath.GetChildFileWithName("cache.db").ToString(),
                    _cacheScheduler));
            Container.RegisterSingleton<IImageCache>(
                () =>
                    new ImageCache(_paths.LocalDataPath.GetChildFileWithName("image.cache.db").ToString(),
                        _cacheScheduler));
            Container.RegisterSingleton<IApiLocalCache>(
                () =>
                    new ApiLocalCache(
                        _paths.LocalDataPath.GetChildFileWithName("api.cache.db").ToString(), _cacheScheduler));
        }

        void RegisterRoamingCaches() {
            Container.RegisterSingleton<IUserCache>(
                () => new UserCache(_paths.RoamingDataPath.GetChildFileWithName("cache.db").ToString(),
                    _cacheScheduler));
            Container.RegisterSingleton<ISecureCache>(
                () =>
                    new SecureCache(_paths.RoamingDataPath.GetChildFileWithName("secure-cache.db").ToString(),
                        Common.IsWindows ? (IEncryptionProvider)new WindowsEncryptionProvider() : new NotWindowsEncryptionProvider(), _cacheScheduler));
        }

        // Until https://github.com/aarnott/pclcrypto
        // https://github.com/akavache/Akavache/issues/190
        public class NotWindowsEncryptionProvider : IEncryptionProvider
        {
            public IObservable<byte[]> EncryptBlock(byte[] block) {
                return Observable.Return(Encoding.UTF8.GetBytes(Convert.ToBase64String(block)));
            }

            public IObservable<byte[]> DecryptBlock(byte[] block) {
                return Observable.Return(Convert.FromBase64String(Encoding.UTF8.GetString(block)));
            }
        }

        public class WindowsEncryptionProvider : IEncryptionProvider
        {
            public IObservable<byte[]> EncryptBlock(byte[] block) {
                return Observable.Return(ProtectedData.Protect(block, null, DataProtectionScope.CurrentUser));
            }

            public IObservable<byte[]> DecryptBlock(byte[] block) {
                return Observable.Return(ProtectedData.Unprotect(block, null, DataProtectionScope.CurrentUser));
            }
        }


        void RegisterCache<T>(T cache) where T : IBlobCache {
            var cacheManager = Container.GetInstance<ICacheManager>();
            cacheManager.RegisterCache(cache);
        }

        protected virtual void RegisterViews() {}

        void RunCommands() => EnvironmentExit(Container.GetInstance<CommandRunner>().RunCommandsAndLog(_args));

        protected abstract void EnvironmentExit(int exitCode);

        protected virtual IEnumerable<Assembly> GetPresentationAssemblies()
            =>
            pluginAssemblies.Concat(platformAssemblies)
                .Concat(
                    assemblyPath.DirectoryInfo.GetFiles("withSIX.Core.Presentation.Bridge.dll")
                        .Select(x => x.FullName)
                        .Select(AssemblyLoadFrom))
                .Concat(globalPresentationAssemblies);

        protected virtual void RegisterServices() {
            RegisterRegisteredServices();

            RegisterPlugins<INotificationProvider>(_presentationAssemblies, Lifestyle.Singleton);
            var assemblies = GetAllAssemblies().ToArray();
            RegisterPlugins<IInitializer>(assemblies, Lifestyle.Singleton);
            RegisterPlugins<IHandleExceptionPlugin>(assemblies, Lifestyle.Singleton);
            RegisterPlugins<Profile>(assemblies);
            // , Lifestyle.Singleton // fails
            Container.RegisterSingleton<IToolsInstaller>(
                () =>
                    new ToolsInstaller(Container.GetInstance<IFileDownloader>(),
                        new Restarter(Container.GetInstance<IShutdownHandler>(),
                            Container.GetInstance<IDialogManager>()),
                        _paths.ToolPath));

            Container.RegisterSingleton(new PremiumDelegate(_isPremium));
            Container.RegisterSingleton<IContentInstallationService>(
                () =>
                    new ContentInstaller(evt => evt.Raise(), Container.GetInstance<IGameLocker>(),
                        Container.GetInstance<IINstallerSessionFactory>()));

            // LEGACY
            Container.RegisterSingleton<IPathConfiguration>(() => Common.Paths);

            Container.RegisterInitializer<IProcessManager>(ConfigureProcessManager);

            RegisterMediator();
            RegisterMessageBus();
            withSIX.Core.Presentation.AppBootstrapper.StartSQLite();
            RegisterDownloader();
            RegisterTools();
            RegisterCaches();
        }

        protected IEnumerable<Assembly> GetAllAssemblies() => new[] {
                pluginAssemblies, globalPresentationAssemblies, GetInfraAssemblies, _applicationAssemblies,
                coreAssemblies
            }
            .SelectMany(x => x).Distinct();

        protected abstract void RegisterMessageBus();

        void RegisterRegisteredServices() {
            RegisterSingleAllInterfaces<IDomainService>(pluginAssemblies.Concat(coreAssemblies));
            RegisterSingleAllInterfaces<IApplicationService>(pluginAssemblies.Concat(_applicationAssemblies));
            RegisterSingleAllInterfaces<IInfrastructureService>(pluginAssemblies.Concat(GetInfraAssemblies));
            RegisterSingleAllInterfaces<IPresentationService>(_presentationAssemblies);
            RegisterAllInterfaces<ITransientService>(_presentationAssemblies);
        }

        protected abstract void RegisterAllInterfaces<T>(IEnumerable<Assembly> assemblies);

        protected abstract void RegisterSingleAllInterfaces<T>(IEnumerable<Assembly> assemblies);

        void RegisterMediator() {
            Container.RegisterSingleton(new SingleInstanceFactory(Container.GetInstance));
            Container.RegisterSingleton(new MultiInstanceFactory(Container.GetAllInstances));
            Container.RegisterSingleton<IMediator, Mediator>();

            RegisterRequestHandlers(typeof(IAsyncRequestHandler<,>),
                typeof(IRequestHandler<,>), typeof(ICancellableAsyncRequestHandler<,>));
            RegisterNotificationHandlers(typeof(INotificationHandler<>),
                typeof(IAsyncNotificationHandler<>), typeof(ICancellableAsyncNotificationHandler<>));

            // Decorators execute in reverse-order. So the last one executes first, and so forth.
            Container.RegisterDecorator<IMediator, ActionNotifierDecorator>(Lifestyle.Singleton);
            Container.RegisterDecorator<IMediator, DbScopeDecorator>(Lifestyle.Singleton);
            Container.RegisterDecorator<IMediator, MediatorValidationDecorator>(Lifestyle.Singleton);
            Container.RegisterDecorator<IMediator, GameWriteLockDecorator>(Lifestyle.Singleton);
            if (Common.AppCommon.Type < ReleaseType.Beta)
                Container.RegisterDecorator<IMediator, MediatorLoggingDecorator>(Lifestyle.Singleton);
            //Container.RegisterDecorator<IMediator, RequestMemoryDecorator>(Lifestyle.Singleton);
        }

        void RegisterRequestHandlers(params Type[] types) {
            foreach (var h in types) {
                try {
                    Container.Register(h, _applicationAssemblies.Concat(GetInfraAssemblies), Lifestyle.Singleton);
                } catch (Exception ex) {
                    throw;
                }
                // TODO: Infra should not contain use cases. It's only here because CE is using Mediator to construct services: Not what it is designed for!
            }
        }

        void RegisterNotificationHandlers(params Type[] types) {
            foreach (var h in types) {
                var assemblies = _applicationAssemblies.Concat(GetInfraAssemblies).ToArray();
                Container.RegisterCollection(h, assemblies);
                // TODO: Infra should not contain use cases. It's only here because CE is using Mediator to construct services: Not what it is designed for!
            }
        }

        void RegisterDownloader() {
            RegisterDownloaderFactories();

            Container.RegisterSingleton<IZsyncLauncher, ZsyncLauncher>();
            Container.RegisterSingleton<IRsyncLauncher, RsyncLauncher>();
            Container.RegisterSingleton<RsyncOutputParser>();
            Container.RegisterSingleton<ZsyncOutputParser>();
            Container.RegisterSingleton<ICopyFile, FileCopier>();

            Container.Register<IHostChecker, HostChecker>();
            Container.Register<IHostCheckerWithPing, HostCheckerWithPing>();

            RegisterPlugins<IDownloadProtocol>(coreAssemblies,
                Lifestyle.Singleton);
            RegisterPlugins<IUploadProtocol>(coreAssemblies,
                Lifestyle.Singleton);

            Container.RegisterSingleton<IHttpDownloadProtocol, HttpDownloadProtocol>();
            Container.RegisterSingleton<IFileDownloader, FileDownloader>();
            Container.RegisterSingleton<IFileUploader, FileUploader>();
            Container.RegisterSingleton<IStringDownloader, StringDownloader>();
            Container.RegisterSingleton<IDataDownloader, DataDownloader>();
            //_container.Register<IMultiMirrorFileDownloader, MultiMirrorFileDownloader>();
            //_container.Register<IMirrorSelector, ScoreMirrorSelector>(() => new ScoreMirrorSelector());
            Container.RegisterSingleton<IFileDownloadHelper, FileDownloadHelper>();
            Container.RegisterSingleton<IZsyncMake, ZsyncMake>();

            Container.RegisterDecorator<IFileDownloader, LoggingFileDownloaderDecorator>(Lifestyle.Singleton);
            Container.RegisterDecorator<IFileUploader, LoggingFileUploaderDecorator>(Lifestyle.Singleton);

            Container.RegisterSingleton<Func<HostCheckerType>>(() => HostCheckerType.WithPing);
            Container.RegisterSingleton<Func<ProtocolPreference>>(() => ProtocolPreference.Any);

            // TODO: Get the max concurrent download setting... based on premium etc
            //settings.Local.MaxConcurrentDownloads
            Container
                .RegisterSingleton<Func<IMultiMirrorFileDownloader, ExportLifetimeContext<IFileQueueDownloader>>>(
                    x => {
                        var downloader = new MultiThreadedFileQueueDownloader(SyncEvilGlobal.Limiter, x);
                        return new ExportLifetimeContext<IFileQueueDownloader>(downloader, TaskExt.NullAction);
                    });
        }

        void RegisterDownloaderFactories() {
            // TODO: this is a replacement for the simple factory classes we have, they can be removed later?
            Container.RegisterSingleton<Func<IMirrorSelector, ExportLifetimeContext<IMultiMirrorFileDownloader>>>(
                x => new ExportLifetimeContext<IMultiMirrorFileDownloader>(
                    new MultiMirrorFileDownloader(Container.GetInstance<IFileDownloader>(), x), TaskExt.NullAction));
            Container.RegisterSingleton<Func<IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>>>(
                x => {
                    var hostChecker =
                        Container.GetInstance<Func<ExportLifetimeContext<IHostChecker>>>().Invoke();
                    var selector = new ScoreMirrorSelector(hostChecker.Value, x);
                    return new ExportLifetimeContext<IMirrorSelector>(selector, () => {
                        selector.Dispose();
                        hostChecker.Dispose();
                    });
                });
            Container
                .RegisterSingleton<Func<int, IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>>>(
                    (limit, x) => {
                        var hostChecker =
                            Container.GetInstance<Func<ExportLifetimeContext<IHostChecker>>>().Invoke();
                        var selector = new ScoreMirrorSelector(limit, hostChecker.Value, x);
                        return new ExportLifetimeContext<IMirrorSelector>(selector, () => {
                            selector.Dispose();
                            hostChecker.Dispose();
                        });
                    });
            Container.RegisterSingleton<Func<ExportLifetimeContext<IHostChecker>>>(() => {
                var hostCheckerType = Container.GetInstance<Func<HostCheckerType>>();
                var hostChecker = hostCheckerType() == HostCheckerType.WithPing
                    ? Container.GetInstance<IHostCheckerWithPing>()
                    : Container.GetInstance<IHostChecker>();
                return new ExportLifetimeContext<IHostChecker>(hostChecker, TaskExt.NullAction);
            });

            Container.RegisterSingleton<Func<ExportLifetimeContext<IWebClient>>>(() => {
                var wc = Container.GetInstance<Func<IWebClient>>();
                var webClient = wc();
                return new ExportLifetimeContext<IWebClient>(webClient, webClient.Dispose);
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
            Container.RegisterSingleton(() => new Lazy<IWCFClient>());
            Container.RegisterSingleton(() => Tools.Generic);
            Container.RegisterSingleton(() => Tools.FileUtil);
            Container.RegisterSingleton<Tools.FileTools.IFileOps>(() => Tools.FileUtil.Ops);
            Container.RegisterSingleton(() => Tools.Compression);
            Container.RegisterSingleton(() => Tools.Compression.Gzip);
            Container.RegisterSingleton(() => Tools.HashEncryption);
            Container.RegisterSingleton(() => Tools.Serialization);
            Container.RegisterSingleton(() => Tools.Serialization.Json);
            Container.RegisterSingleton(() => Tools.Transfer);
        }

        protected Task End() => TaskExt.StartLongRunningTask(EndInternal);

        private async Task EndInternal() {
            await RunDeinitializers().ConfigureAwait(false);
            await Container.GetInstance<ICacheManager>().Shutdown().ConfigureAwait(false);
        }

        async Task RunDeinitializers() {
            foreach (var i in _initializers)
                await i.Deinitialize().ConfigureAwait(false);
        }

        protected virtual async Task AfterUi() {
            //await Container.GetInstance<ICacheManager>().VacuumIfNeeded(TimeSpan.FromDays(14)).ConfigureAwait(false);

            if (Common.IsWindows)
                await HandleStartWithWindows().ConfigureAwait(false);

            foreach (var i in _initializers.OfType<IInitializeAfterUI>())
                await i.InitializeAfterUI().ConfigureAwait(false);
        }

        private async Task HandleStartWithWindows() {
            var settings =
                await
                    Container.GetInstance<IDbContextLocator>()
                        .GetSettingsContext()
                        .GetSettings()
                        .ConfigureAwait(false);
            new StartWithWindowsHandler().HandleStartWithWindows(settings.Local.StartWithWindows);
        }

        protected virtual void BackgroundActions() {
            InstallToolsInBackground();
            //HandleImportInBackground();
        }

        // TODO: Should only import after we have synchronized collection content etc??
        // Or we create LocalContent as temporary placeholder, and then in the future we should look at converting to network content then??
        // TODO
        //void HandleImportInBackground() => TryHandleImport();
        /*
        private async Task TryHandleImport() {
            try {
                await TaskExt.StartLongRunningTask(HandleImportInternal).ConfigureAwait(false);
            } catch (Exception ex) {
                await UserError.Throw(ex.Message, ex);
            }
        }*/

        async Task HandleImportInternal() {
            var importer = Container.GetInstance<IPlayWithSixImporter>();
            var dialogManager = Container.GetInstance<IDialogManager>();
            using (var scope = Container.GetInstance<IDbContextFactory>().Create()) {
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
                    var ctx = Container.GetInstance<IDbContextLocator>().GetSettingsContext();
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