// <copyright company="SIX Networks GmbH" file="AppBootstrapperBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Akavache;
using MediatR;
using Newtonsoft.Json;
using SimpleInjector;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Infrastructure;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Cache;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Decorators;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Sync.Core;
using withSIX.Sync.Core.Legacy;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.MirrorSelectors;
using withSIX.Sync.Core.Transfer.Protocols;
using withSIX.Sync.Core.Transfer.Protocols.Handlers;

namespace withSIX.Core.Presentation.Legacy
{
    public abstract class AppBootstrapperBase
    {
        ContainerConfiguration _config;

        protected Container Container { get; private set; }
        private Lazy<IEnumerable<Assembly>> _assemblies;

        protected AppBootstrapperBase() {
             _assemblies = new Lazy<IEnumerable<Assembly>>(() => SelectAssemblies().ToList());
        }

        protected IEnumerable<Assembly> Assemblies => _assemblies.Value;

        protected abstract ContainerConfiguration CreateConfiguration();


        // TODO: Add all assemblies that implement concrete types defined in setups here
        protected virtual IEnumerable<Assembly> SelectAssemblies() => new[] {
            typeof(AppBootstrapperBase).GetTypeInfo().Assembly,
            typeof(CompressionUtil).GetTypeInfo().Assembly,
            typeof(FileWriter).GetTypeInfo().Assembly,
            typeof(Restarter).GetTypeInfo().Assembly,
            typeof(Common).GetTypeInfo().Assembly,
            typeof(Mediator).GetTypeInfo().Assembly
        }.Distinct();

        /*
                         protected AppBootstrapperBase(bool useApplication = true) : base(useApplication) {
                    using (this.Bench())
                        Initialize();
                }
        
                         protected override void Configure() {
                            Container = new Container();
                            _config = new ContainerConfiguration(Container);
                        }
        
                        protected override void StartDesignTime() {
                            base.StartDesignTime();
                            SetupContainer();
                        }
        
                        protected override object GetInstance(Type serviceType, string key) => Container.GetInstance(serviceType);
        
                        protected override IEnumerable<object> GetAllInstances(Type serviceType) {
                            object[] allInstances;
                            try {
                                allInstances = Container.GetAllInstances(serviceType).ToArray();
                            } catch (ActivationException) {
                                return new[] {Container.GetInstance(serviceType)};
                            }
                            // workaround for view search by getallinstances :S
                            if (!allInstances.Any())
                                return new[] {Container.GetInstance(serviceType)};
                            return allInstances;
                        }
        
                        protected override void BuildUp(object instance) {
                            //Container.InjectProperties(instance); // PropertyIjection bad..
                        }
                        */

        protected void SetupContainer() {
            ConfigureContainer();
            SetupGlobalServices();
#if DEBUG
            // Creates an instance of every registered type, so better not use unless testing
            //_container.Verify();
#endif
        }

        protected virtual void ConfigureContainer() {
            Container = new Container();
            _config = CreateConfiguration();
            _config.Setup();
            SetupCaches();
        }

        protected virtual void AfterSetup() {}

        void SetupGlobalServices() {
            // TODO: Get rid of this monstrosity.
            // Reason for being here: UserSettings.Current calls into a dialog
            // and global services are just horrible...
            using (MainLog.Bench(null, "SimpleInjector.AfterSetup")) {
                // Tsk
                AfterSetup();
                RegisterToolServices();
                SyncEvilGlobal.Setup(Container.GetInstance<EvilGlobalServices>(), () => 3); // todo
#if DEBUG
                // Creates an instance of every registered type, so better not use unless testing
                //_container.Verify();
#endif
            }
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

        void SetupCaches() {
            var scheduler = TaskPoolScheduler.Default;

            // Load at least one cache if hacked
            var local =
                new Lazy<ILocalCache>(
                    () =>
                        new LocalCache(Common.Paths.LocalDataPath.GetChildFileWithName("cache.db").ToString(),
                            scheduler));
            // Using custom registration so that we only register caches we actually use...
            Container.RegisterSingleton(() => RegisterCache(local.Value));
            Container.RegisterSingleton<IImageCache>(
                () =>
                    RegisterCache(
                        new ImageCache(Common.Paths.LocalDataPath.GetChildFileWithName("image.cache.db").ToString(),
                            scheduler)));
            Container.RegisterSingleton<IUserCache>(
                () =>
                    RegisterCache(new UserCache(Common.Paths.DataPath.GetChildFileWithName("cache.db").ToString(),
                        scheduler)));
            Container.RegisterSingleton<IInMemoryCache>(() => RegisterCache(new InMemoryCache(scheduler)));
            Container.RegisterSingleton<IApiLocalCache>(
                () =>
                    RegisterCache(
                        new ApiLocalCache(
                            Common.Paths.LocalDataPath.GetChildFileWithName("api.cache.db").ToString(), scheduler)));
            Container.RegisterSingleton<ISecureCache>(
                () =>
                    RegisterCache(
                        new SecureCache(Common.Paths.DataPath.GetChildFileWithName("secure-cache.db").ToString(),
                            Common.IsWindows
                                ? (IEncryptionProvider) new WindowsEncryptionProvider()
                                : new NotWindowsEncryptionProvider(), scheduler)));
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

        public virtual void OnExit(object sender, EventArgs e) {}
        public virtual void OnStartup() {}

        public class WindowsEncryptionProvider : IEncryptionProvider
        {
            public IObservable<byte[]> EncryptBlock(byte[] block) {
                return Observable.Return(ProtectedData.Protect(block, null, DataProtectionScope.CurrentUser));
            }

            public IObservable<byte[]> DecryptBlock(byte[] block) {
                return Observable.Return(ProtectedData.Unprotect(block, null, DataProtectionScope.CurrentUser));
            }
        }

        T RegisterCache<T>(T cache) where T : IBlobCache {
            var cacheManager = Container.GetInstance<ICacheManager>();
            cacheManager.RegisterCache(cache);
            return cache;
        }

        // TODO: Limit assemblies to bare minimum required for the specific Registration?
        // TODO: Stop using AllowOverridingRegistration!
        // TODO: Optimize custom extension methods
        // TODO: Get rid of Lazy/Func/ExportFactory as much as possible
        public abstract class ContainerConfiguration
        {
            readonly IEnumerable<Assembly> _assemblies;
            protected readonly Container _container;

            protected ContainerConfiguration(Container container, IEnumerable<Assembly> assemblies) {
                _container = container;
                _assemblies = assemblies;
            }

            protected abstract void RegisterAllInterfaces<T>(IEnumerable<Assembly> assemblies);

            protected abstract void RegisterSingleAllInterfaces<T>(IEnumerable<Assembly> assemblies);

            protected abstract void RegisterSingleAllInterfacesAndType<T>(IEnumerable<Assembly> assemblies);

            protected abstract void RegisterPlugins<T>(IEnumerable<Assembly> assemblies, Lifestyle style = null) where T : class;


            public void Setup() {
                ConfigureContainer2();
                Register();
            }

            protected abstract void ConfigureContainer2();

            void Register() {
                //RegisterEventAggregator(_container);

                _container.RegisterInitializer<IProcessManager>(ConfigureProcessManager);

                _container.RegisterSingleton<Func<HostCheckerType>>(() => HostCheckerType.WithPing);
                RegisterSingleAllInterfacesAndType<IDomainService>(_assemblies);
                RegisterSingleAllInterfaces<IApplicationService>(_assemblies);
                RegisterSingleAllInterfaces<IInfrastructureService>(_assemblies);
                RegisterSingleAllInterfacesAndType<IPresentationService>(_assemblies);
                RegisterAllInterfaces<ITransientService>(_assemblies);

                _container.RegisterSingleton(JsonSerializer.Create(JsonSupport.DefaultSettings));

                _container.RegisterSingleton<IPathConfiguration>(() => Common.Paths);
                _container.RegisterSingleton<IRestarter>(
                    () =>
                        new Restarter(_container.GetInstance<IShutdownHandler>(),
                            _container.GetInstance<IDialogManager>())); // wth?

                RegisterMediator();
                RegisterDownloader();

                _container.RegisterSingleton<IToolsInstaller>(
                    () =>
                        new ToolsInstaller(_container.GetInstance<IFileDownloader>(),
                            _container.GetInstance<IRestarter>(),
                            Common.Paths.ToolPath));

                _container.RegisterSingleton(
                    () =>
                        new VersionRegistry(CommonUrls.SoftwareUpdateUri,
                            _container.GetInstance<IApiLocalObjectCacheManager>(), _container.GetInstance<ISystemInfo>()));

                RegisterTools();
            }

            void RegisterTools() {
                _container.RegisterSingleton(() => Tools.Generic);
                _container.RegisterSingleton(() => Tools.FileUtil);
                _container.RegisterSingleton<Tools.FileTools.IFileOps>(() => Tools.FileUtil.Ops);
                _container.RegisterSingleton(() => Tools.Compression);
                _container.RegisterSingleton(() => Tools.Compression.Gzip);
                _container.RegisterSingleton(() => Tools.HashEncryption);
                _container.RegisterSingleton(() => Tools.Serialization);
                _container.RegisterSingleton(() => Tools.Serialization.Json);
                _container.RegisterSingleton(() => Tools.Transfer);
            }

            void RegisterMediator() {
                _container.RegisterSingleton(new SingleInstanceFactory(_container.GetInstance));
                _container.RegisterSingleton(new MultiInstanceFactory(_container.GetAllInstances));
                _container.RegisterSingleton<IMediator, Mediator>();

                RegisterRequestHandlers();
                RegisterNotificationHandlers();
                RegisterMediatorDecorators();
            }

            protected abstract void RegisterMediatorDecorators();

            void RegisterRequestHandlers() {
                var requestHandlers = new[] {
                    typeof(IAsyncRequestHandler<,>),
                    typeof(ICancellableAsyncRequestHandler<,>),
                    typeof(IRequestHandler<,>)
                };

                foreach (var h in requestHandlers)
                    _container.Register(h, _assemblies, Lifestyle.Singleton);
            }

            void RegisterNotificationHandlers() {
                var notificationHandlers = new[] {
                    typeof(INotificationHandler<>),
                    typeof(IAsyncNotificationHandler<>),
                    typeof(ICancellableAsyncNotificationHandler<>),
                    typeof(IPipelineBehavior<,>)
                };

                foreach (var h in notificationHandlers) {
                    //_container.Register(h, _assemblies, Lifestyle.Singleton);
                    //_container.RegisterSingleAllInterfacesAndType<>();
                    _container.RegisterCollection(h, _assemblies);
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

                RegisterPlugins<IDownloadProtocol>(_assemblies, Lifestyle.Singleton);
                RegisterPlugins<IUploadProtocol>(_assemblies, Lifestyle.Singleton);

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

                _container.RegisterSingleton<Func<ExportLifetimeContext<IWebClient>>>(() => {
                    var wc = _container.GetInstance<Func<IWebClient>>();
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
                if (Common.Flags.Verbose) {
                    processMan.Launched.Subscribe(
                        launched =>
                            MainLog.Logger.Debug("::ProcessManager:: Launched {0}. {1} {2} from {3}]", launched.Item2,
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
                }
            }
        }
    }
}