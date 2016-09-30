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
using System.Windows;
using System.Windows.Media;
using AutoMapper;
using Caliburn.Micro;
using ReactiveUI;
using MediatR;
using SimpleInjector;


using withSIX.Api.Models;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.ContentEngine.Infra.Services;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Core.Presentation.Extensions;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Core.Presentation.Wpf;
using SN.withSIX.Core.Presentation.Wpf.Legacy;
using SN.withSIX.Core.Presentation.Wpf.Services;
using SN.withSIX.Core.Presentation.Wpf.Views.Dialogs;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Play.Presentation.Wpf.Services;
using SN.withSIX.Sync.Core.Repositories;
using SN.withSIX.Sync.Core.Transfer;
using Splat;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;
using withSIX.Play.Applications;
using withSIX.Play.Applications.NotificationHandlers;
using withSIX.Play.Applications.Services;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Applications.UseCases;
using withSIX.Play.Applications.UseCases.Games;
using withSIX.Play.Applications.ViewModels;
using withSIX.Play.Applications.ViewModels.Games.Dialogs;
using withSIX.Play.Core;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Games.Legacy.Servers;
using withSIX.Play.Core.Games.Services;
using withSIX.Play.Core.Options;
using withSIX.Play.Infra.Api;
using withSIX.Play.Infra.Data.Services;
using withSIX.Play.Infra.Server.Hubs;
using withSIX.Play.Infra.Server.UseCases;
using ViewLocator = Caliburn.Micro.ViewLocator;

namespace SN.withSIX.Play.Presentation.Wpf
{
    public class AppBootstrapper : WpfAppBootstrapper<IPlayShellViewModel>
    {
        static Func<Type, DependencyObject, object, Type> _originalLocateTypeForModelType;
        static readonly Assembly[] presentationAssemblies = {
            Assembly.GetExecutingAssembly(),
            typeof (EnterConfirmView).Assembly,
            typeof (CompressionUtil).Assembly,
            typeof (UserSettingsStorage).Assembly
        };
        IPlayStartupManager _startupManager;

        protected override void SetupViewNamespaces() {
            base.SetupViewNamespaces();

            var concepts = new[] {"Connect", "Games", "Games.Library"};
            var types = new[] {"Overlays", "Popups", "Dialogs"};

            var nsRootSource = "withSIX.Play.Applications.ViewModels";
            var nsRootTarget = "withSIX.Play.Presentation.Wpf.Views";

            // Setup root
            ViewLocator.AddNamespaceMapping(nsRootSource, nsRootTarget);
            foreach (var type in types)
                ViewLocator.AddNamespaceMapping(nsRootSource + "." + type, nsRootTarget + "." + type);

            // Setup concepts
            foreach (var concept in concepts) {
                var nsSource = nsRootSource + "." + concept;
                var nsTarget = nsRootTarget + "." + concept;
                ViewLocator.AddNamespaceMapping(nsSource, nsTarget);
                foreach (var type in types)
                    ViewLocator.AddNamespaceMapping(nsSource + "." + type, nsTarget + "." + type);
            }
        }

        protected override void SetupRx(IMutableDependencyResolver dependencyResolver) {
            base.SetupRx(dependencyResolver);
            var viewInterfaceFilterType = typeof (IViewFor);
            dependencyResolver.RegisterAllInterfaces<IViewFor>(presentationAssemblies,
                (type, type1) => viewInterfaceFilterType.IsAssignableFrom(type));
            //Container.RegisterSingleton<IScreen>(Container.GetInstance<IPlayShellViewModel>);
        }

        protected override IEnumerable<Assembly> SelectAssemblies() => presentationAssemblies.Concat(new[] {
                typeof (PlayShellViewModel).Assembly,
                typeof (GameMapperConfig).Assembly,
                typeof (ScreenBase).Assembly,
                typeof (StartupManager).Assembly,
                typeof (ConnectionManager).Assembly,
                typeof (UserSettingsStorage).Assembly,
                typeof (ListGamesQuery).Assembly,
                typeof (SoftwareUpdate).Assembly,
                typeof (Repository).Assembly,
                typeof (Game).Assembly,
                typeof (ContactList).Assembly,
                typeof (WCFClient).Assembly,
                typeof (WindowManager).Assembly,
                typeof (BaseHub).Assembly,
                typeof (IContentEngineGameContext).Assembly,
                typeof (IContentEngine).Assembly
            }).Concat(base.SelectAssemblies()).Distinct();

        protected override void PreStart() {
            // TODO: How to deal with other mappers?
            var c = new MapperConfiguration(cfg => {
                cfg.SetupConverters();
                cfg.CreateMap<Collection, CollectionInfo>()
                    .Include<CustomCollection, CollectionInfo>()
                    .ForMember(x => x.ShortId, opt => opt.MapFrom(src => new ShortGuid(src.Id)));
                cfg.CreateMap<CustomCollection, CollectionInfo>();
                cfg.CreateMap<CustomCollection, CollectionImageDataModel>();
            });

            MappingExtensions.Mapper = c.CreateMapper();
            _startupManager = Container.GetInstance<IPlayStartupManager>();
            StartupSequence.Start(_startupManager);
            //SetupCookies(Container.GetInstance<IInstalledGamesService>());
            SetupThemes();
            // WARNING - DO NOT SHOW NON-FATAL DIALOGS HERE
        }

        void SetupThemes() {
            ShowNewProfileDialogCommandHandler.Accents =
                ThemeInfo.Accents.Select(x => x.ColorBrush).OfType<SolidColorBrush>()
                    .Select(x => x.Color.ToString()).ToArray();
        }

        protected override async Task ExitAsync(object o, EventArgs args) {
            await base.ExitAsync(o, args).ConfigureAwait(false);
            // TODO: The task.Run is afaik to not end up blocking the calling thread and ending in dead-lock
            // Probably better to deal with that in the startupmanager itself, localized at the relevant spot?
            if (_startupManager != null)
                await Task.Run(() => StartupSequence.Exit(_startupManager)).ConfigureAwait(false);
            else
                MainLog.Logger.Error("StartupManager was null on exit!");
        }

        protected override void Exit(object o, EventArgs args) {
            // TODO: Cleanup (This must probably run on the UI Thread...thats why its here)
            SixCefStart.Exit();
        }

        protected override void ConfigureContainer() {
            base.ConfigureContainer();

            Container.RegisterSingleton<IConnectionManager>(
                () => new ConnectionManager(CommonUrls.SignalrApi));
            Container.RegisterSingleton<Func<ProtocolPreference>>(
                () => Container.GetInstance<UserSettings>().AppOptions.ProtocolPreference);
            Container.RegisterSingleton<IPlayStartupManager, PlayStartupManager>();
            Container.RegisterSingleAllInterfaces<IAMapper>(AssemblySource.Instance);
            Container.RegisterSingleton<IExceptionHandler, PlayExceptionHandler>();
            Container.RegisterSingleton(() => new Lazy<IPlayShellViewModel>(Container.GetInstance<IPlayShellViewModel>));
            Container.RegisterSingleton(() => DomainEvilGlobal.Settings);
            Container.RegisterSingleton(() => DomainEvilGlobal.LocalMachineInfo);
            Container
                .RegisterSingleton<Func<IMultiMirrorFileDownloader, ExportLifetimeContext<IFileQueueDownloader>>>(
                    x => {
                        var appOptions = Container.GetInstance<UserSettings>().AppOptions;
                        var downloader = new MultiThreadedFileQueueDownloader(appOptions.GetMaxThreads, x);
                        return new ExportLifetimeContext<IFileQueueDownloader>(downloader, TaskExt.NullAction);
                    });

            Container.RegisterSingleton(() => {
                var appOptions = Container.GetInstance<UserSettings>().AppOptions;
                return new SelfUpdater(() => appOptions.EnableBetaUpdates,
                    Container.GetInstance<IEventAggregator>(), Container.GetInstance<IProcessManager>(),
                    Container.GetInstance<IFileDownloader>(),
                    Container.GetInstance<IMediator>(),
                    Container.GetInstance<ExportFactory<IWebClient>>(), Container.GetInstance<IRestarter>());
            });

            Container.RegisterSingleton<ISelfUpdater>(
                () => Container.GetInstance<SelfUpdater>());

            Container.RegisterSingleton<Func<ISupportServers, ExportLifetimeContext<IServerList>>>(
                x => {
                    var sl = new ServerList(x, Container.GetInstance<UserSettings>(),
                        Container.GetInstance<IEventAggregator>(),
                        Container.GetInstance<IGameServerQueryHandler>(), Container.GetInstance<IGameContext>());
                    return new ExportLifetimeContext<IServerList>(sl, sl.Dispose);
                });

            Container.RegisterSingleton<NotificationCenterMessageHandler>();
            //Container.RegisterSingleton<INotificationCenterMessageHandler, NotificationCenterMessageHandler>(); // Does not work as intended
            Container.RegisterSingleton<INotificationCenterMessageHandler>(
                () => Container.GetInstance<NotificationCenterMessageHandler>());

            Container.RegisterSingleton<IUserSettingsStorage>(
                () =>
                    new UserSettingsStorage(
                        (ex, location) => Container.GetInstance<IDialogManager>().ExceptionDialog(ex,
                            $"An error occurred while trying to load Settings from: {location}\nIf you continue you will loose your settings, but we will at least make a backup for you.")));

            Container.RegisterDecorator<IMediator, MediatorApiContextDecorator>(Lifestyle.Singleton);

        }

        protected override void AfterSetup() {
            base.AfterSetup();

            Cheat.SetServices(Container.GetInstance<ICheatImpl>());
            CalculatedGameSettings.RaiseEvent = Cheat.PublishEvent;
            CalculatedGameSettings.NotifyEnMass = async (message) => {
                await Cheat.PublishDomainEvent((dynamic) message).ConfigureAwait(false);
                Cheat.PublishEvent(message);
            };

            SetupSettings();
            DomainEvilGlobal.SelectedGame = Container.GetInstance<EvilGlobalSelectedGame>();

            var authProviderStorage = Container.GetInstance<IAuthProviderStorage>();
            var authProvider = Container.GetInstance<IAuthProvider>();
            PwsUriHandler.GetAuthInfoFromUri = authProvider.GetAuthInfoFromUri;
            PwsUriHandler.SetAuthInfo = authProviderStorage.SetAuthInfo;

            var gameContext = Container.GetInstance<IGameContext>();
            var recentGameSet = DomainEvilGlobal.Settings.GameOptions.RecentGameSet;
            DomainEvilGlobal.SelectedGame.ActiveGame = recentGameSet == null
                ? FindFirstInstalledGameOrDefault(gameContext)
                : gameContext.Games.Find(recentGameSet.Id) ??
                  FindFirstInstalledGameOrDefault(gameContext);
        }

        void SetupSettings() {
            DomainEvilGlobal.SecretData = Task.Run(() => BuildSecretData()).Result;
            // TODO: Who uses Notes anyway?
            DomainEvilGlobal.NoteStorage = new NoteStorage();
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

        static Game FindFirstInstalledGameOrDefault(IGameContext gameContext) => gameContext.Games.LastOrDefault(x => x.InstalledState.IsInstalled)
       ?? gameContext.Games.FindOrThrow(GameGuids.Arma3);

        protected override void StartDesignTime() {
            base.StartDesignTime();

            if (_originalLocateTypeForModelType == null)
                _originalLocateTypeForModelType = ViewLocator.LocateTypeForModelType;
            ViewLocator.LocateTypeForModelType = (type, o, arg3) => {
                var t = _originalLocateTypeForModelType(type, o, arg3);
                if (t != null)
                    return t;

                return type.Name.StartsWith("DesignTime")
                    ? _originalLocateTypeForModelType(
                        SelectDesignTimeFallbackAssemblies()
                            .FirstOrDefault(y => y.Name == type.Name.Replace("DesignTime", "")),
                        o, arg3)
                    : null;
            };
        }

        static IEnumerable<Type> SelectDesignTimeFallbackAssemblies() => typeof(PlayShellViewModel).Assembly.GetTypes().Concat(typeof(WpfDialogManager).Assembly.GetTypes());
    }
}