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
using SimpleInjector.Extensions.ExecutionContextScoping;
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
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Mini.Infra.Data.Services;
using withSIX.Mini.Presentation.Core.Commands;
using withSIX.Mini.Presentation.Core.Services;
using withSIX.Steam.Core;
using withSIX.Steam.Core.Services;
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
    public static class BootstrapperExt
    {
        public static IEnumerable<Assembly> GetAssemblies(this IEnumerable<Type> types)
            => types.Select(x => x.GetTypeInfo().Assembly).Distinct();
    }

    public abstract class AppBootstrapper : IDisposable
    {
        // = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).ToAbsoluteDirectoryPath();
        static readonly IAbsoluteDirectoryPath assemblyPath = CommonBase.AssemblyLoader.GetNetEntryPath();
        private static readonly Assembly[] coreAssemblies = GetCoreTypes().GetAssemblies().ToArray();
        private static readonly Assembly[] infraAssemblies = GetInfraTypes().GetAssemblies().ToArray();
        private static readonly Assembly[] globalPresentationAssemblies =
            GetGlobalPresentationTypes().GetAssemblies().ToArray();
        static readonly Assembly[] globalApplicationAssemblies = GetGlobalAppTypes().GetAssemblies().ToArray();
        private readonly string[] _args;
        protected readonly Container Container;
        private Assembly[] _applicationAssemblies;
        TaskPoolScheduler _cacheScheduler;
        IEnumerable<IInitializer> _initializers;
        private Func<bool> _isPremium;
        Paths _paths;
        private Assembly[] _platformAssemblies;
        private Assembly[] _pluginAssemblies;
        private Assembly[] _presentationAssemblies;

        protected AppBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath;
            _args = args;
            Container = new Container();
        }

        public IAbsoluteDirectoryPath RootPath { get; set; }

        protected virtual IEnumerable<Assembly> GetInfraAssemblies
            =>
                new[] {AssemblyLoadFrom(RootPath.GetChildFileWithName("withSIX.Mini.Presentation.Owin.Core.dll"))}
                    .Concat(
                        infraAssemblies);

        public bool CommandMode { get; set; }

        private BackgroundTasks BackgroundTasks { get; } = new BackgroundTasks();

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        static IEnumerable<Type> GetCoreTypes() {
            yield return typeof(Game);
            yield return typeof(IDomainService);
            yield return typeof(Tools);
            yield return typeof(Package);
            yield return typeof(IContentEngine);
        }

        static IEnumerable<Type> GetInfraTypes() {
            yield return typeof(SteamServiceSession);
            yield return typeof(GameContext);
            yield return typeof(ImageCacheManager);
            yield return typeof(IContentEngineGameContext);
        }

        static IEnumerable<Type> GetGlobalPresentationTypes() {
            yield return typeof(AppBootstrapper);
            yield return typeof(IPresentationService);
        }

        static IEnumerable<Type> GetGlobalAppTypes() {
            yield return typeof(GameSettingsApiModel);
            yield return typeof(IDialogManager);
        }


        public virtual void Configure() {
            _pluginAssemblies = DiscoverAndLoadPlugins().Distinct().ToArray();
            _platformAssemblies = DiscoverAndLoadPlatform().Distinct().ToArray();

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

        protected abstract void LowInitializer();

        protected virtual IEnumerable<Assembly> GetApplicationAssemblies() => globalApplicationAssemblies;

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                Common.Flags.ShuttingDown = true;
                if (!CommandMode)
                    EndOv();
                Container.Dispose();
            }
        }

        ~AppBootstrapper() {
            Dispose(false);
        }

        protected virtual void EndOv() => End().WaitAndUnwrapException();

        protected virtual void ConfigureInstances() {
            Game.SteamHelper = SteamHelper.Create(); // TODO: Move
            DataCheat.Instance = Container.GetInstance<ICallContextService>();
            CoreCheat.SetServices(Container.GetInstance<ICoreCheatImpl>());
            Tools.RegisterServices(Container.GetInstance<ToolsServices>());
            SyncEvilGlobal.Setup(Container.GetInstance<EvilGlobalServices>(), () => _isPremium() ? 6 : 3);
        }

        protected virtual void ConfigureUiInstances() {
            AppBootstrapperExt.StartSQLite();
            RequestScopeService.Instance = new RequestScopeService(Container);
            var bridge = Container.GetInstance<IBridge>();
            GameContextJsonImplementation.Settings = bridge.GameContextSettings();
            Cheat.SetServices(Container.GetInstance<ICheatImpl>());
            Tools.FileTools.FileOps.ShouldRetry = async (s, s1, e) => {
                var result =
                    await
                        UserErrorHandler.HandleUserError(new UserErrorModel(s, s1, RecoveryCommands.YesNoCommands, null,
                            e));
                return result == RecoveryOptionResultModel.RetryOperation;
            };
            Tools.InformUserError = (s, s1, e) => UserErrorHandler.HandleUserError(new InformationalUserError(e, s1, s));
            _initializers = Container.GetAllInstances<IInitializer>();
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
            ConfigureInstances();

            if (CommandMode)
                RunCommands();
            else
                await StartupUiMode(act).ConfigureAwait(false);
        }

        private async Task StartupUiMode(Func<Task> act) {
            try {
                ConfigureUiInstances();
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
            await
                SetupApiPort(settings, settingsStorage, Container.GetInstance<IProcessManager>()).ConfigureAwait(false);
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

        private static async Task SetupApiPort(Settings settings, ISettingsStorage settingsStorage, IProcessManager pm) {
            if (Cheat.Args.Port.HasValue && settings.Local.ApiPort != Cheat.Args.Port) {
                settings.Local.ApiPort = Cheat.Args.Port;
                await settingsStorage.SaveChanges().ConfigureAwait(false);
            }
            Consts.ApiPort = settings.ApiPort;
            var pi = new PortsInfo(pm, Consts.HttpAddress, Consts.HttpsAddress, Consts.CertThumb);
            if (!pi.IsCertRegistered)
                await WindowsApiPortHandler.SetupApiPort(Consts.HttpAddress, Consts.HttpsAddress, pm)
                    .ConfigureAwait(false);
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
            Container.Options.DefaultScopedLifestyle = new ExecutionContextScopeLifestyle();
            ConfigureContainer();
            RegisterServices();
            RegisterViews();
            RegisterApi(Container, async () => {
                var sContext = Container.GetInstance<IDbContextLocator>().GetSettingsContext();
                return (await sContext.GetSettings().ConfigureAwait(false)).Secure.Login?.Authentication.AccessToken;
            });

            if (CommandMode)
                RegisterPlugins<BaseCommand>(_presentationAssemblies);

            var serviceReg = new ServiceRegisterer(Container);
            foreach (var t in GetTypes<ServiceRegistry>(_pluginAssemblies))
                Activator.CreateInstance(t, serviceReg);

            Container.Register<IInstallerSession, InstallerSession>();
        }

        public static void RegisterApi(Container c, Func<Task<string>> authGetter) {
            c.RegisterSingleton(W6Api.Create(authGetter));
            c.RegisterSingleton(W6Api.Create());
            c.RegisterSingleton<IW6Api>(
                () => new W6Api(c.GetInstance<IW6MainApi>(), c.GetInstance<IW6CDNApi>(), W6Api.CreatePolicy()));
        }


        protected abstract IEnumerable<Type> GetTypes<T>(IEnumerable<Assembly> assemblies);

        protected abstract void RegisterPlugins<T>(IEnumerable<Assembly> assemblies, Lifestyle style = null)
            where T : class;

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
                        Common.IsWindows
                            ? (IEncryptionProvider) new WindowsEncryptionProvider()
                            : new NotWindowsEncryptionProvider(), _cacheScheduler));
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
                _pluginAssemblies.Concat(_platformAssemblies)
                    .Concat(
                        assemblyPath.DirectoryInfo.GetFiles("withSIX.Core.Presentation.Bridge.dll")
                            .Select(x => x.FullName)
                            .Select(AssemblyLoadFrom))
                    .Concat(globalPresentationAssemblies);

        protected virtual void RegisterServices() {
            RegisterRegisteredServices();
            Container.RegisterValidation(GetApplicationAssemblies().Concat(_pluginAssemblies));

            RegisterPlugins<INotificationProvider>(_presentationAssemblies, Lifestyle.Singleton);
            var assemblies = GetAllAssemblies().ToArray();
            RegisterPlugins<IInitializer>(assemblies, Lifestyle.Singleton);
            RegisterPlugins<IHandleExceptionPlugin>(assemblies, Lifestyle.Singleton);
            RegisterPlugins<Profile>(assemblies);
            Container.RegisterSingleton<ISteamServiceSession, SteamServiceSessionSignalR>();

            // , Lifestyle.Singleton // fails
            Container.RegisterSingleton<IToolsInstaller>(
                () =>
                    new ToolsInstaller(Container.GetInstance<IFileDownloader>(),
                        new Restarter(Container.GetInstance<IShutdownHandler>(),
                            Container.GetInstance<IDialogManager>()),
                        _paths.ToolPath));

            Container.RegisterSingleton<PremiumDelegate>(() => _isPremium());
            Container.RegisterSingleton<EventRaiser>(evt => evt.Raise());
            Container.RegisterSingleton<IContentInstallationService, ContentInstaller>();
            Container.Register<IRequestScope, RequestScope>(Lifestyle.Scoped);
            Container.RegisterSingleton<IRequestScopeLocator, RequestScopeService>();

            // LEGACY
            Container.RegisterSingleton<IPathConfiguration>(() => Common.Paths);

            Container.RegisterInitializer<IProcessManager>(ConfigureProcessManager);

            RegisterMediator();
            RegisterMessageBus();
            RegisterDownloader();
            RegisterTools();
            RegisterCaches();
        }


        protected IEnumerable<Assembly> GetAllAssemblies() => new[] {
                _pluginAssemblies, globalPresentationAssemblies, GetInfraAssemblies, _applicationAssemblies,
                coreAssemblies
            }
            .SelectMany(x => x).Distinct();

        protected abstract void RegisterMessageBus();

        void RegisterRegisteredServices() {
            RegisterSingleAllInterfaces<IDomainService>(_pluginAssemblies.Concat(coreAssemblies));
            RegisterSingleAllInterfaces<IApplicationService>(_pluginAssemblies.Concat(_applicationAssemblies));
            RegisterSingleAllInterfaces<IInfrastructureService>(_pluginAssemblies.Concat(GetInfraAssemblies));
            RegisterSingleAllInterfaces<IPresentationService>(_presentationAssemblies);
            RegisterAllInterfaces<ITransientService>(_presentationAssemblies);
        }

        protected abstract void RegisterAllInterfaces<T>(IEnumerable<Assembly> assemblies);

        protected abstract void RegisterSingleAllInterfaces<T>(IEnumerable<Assembly> assemblies);

        void RegisterMediator() {
            Container.RegisterMediator(_applicationAssemblies.Concat(GetInfraAssemblies).ToArray());

            // Decorators execute in reverse-order. So the last one executes first, and so forth.
            Container.RegisterDecorator<IMediator, ActionNotifierDecorator>(Lifestyle.Singleton);
            Container.RegisterDecorator<IMediator, DbScopeDecorator>(Lifestyle.Singleton);
            Container.RegisterDecorator<IMediator, MediatorValidationDecorator>(Lifestyle.Singleton);
            Container.RegisterDecorator<IMediator, GameWriteLockDecorator>(Lifestyle.Singleton);
            if (Common.AppCommon.Type < ReleaseType.Beta)
                Container.RegisterDecorator<IMediator, MediatorLoggingDecorator>(Lifestyle.Singleton);
            //Container.RegisterDecorator<IMediator, RequestMemoryDecorator>(Lifestyle.Singleton);
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