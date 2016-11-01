// <copyright company="SIX Networks GmbH" file="ContainerSetup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapper;
using MediatR;
using SimpleInjector;
using SimpleInjector.Extensions.ExecutionContextScoping;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Services;
using withSIX.Core.Presentation;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Bridge.Extensions;
using withSIX.Core.Presentation.Decorators;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Infra.Api;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Commands;
using withSIX.Steam.Api;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Core;
using withSIX.Steam.Core.Services;
using withSIX.Steam.Plugin.Arma;
using withSIX.Steam.Presentation.Commands;
using withSIX.Steam.Presentation.Services;
using withSIX.Steam.Presentation.Usecases;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;
using SteamApi = withSIX.Steam.Api.Services.SteamApi;

namespace withSIX.Steam.Presentation
{
    public class ContainerSetup : IDisposable
    {
        private IReadOnlyCollection<Assembly> _assemblies;
        private Container _container;

        public ContainerSetup(Func<ISteamApi> steamApi) {
            SetupContainer(steamApi);
            CreateInstances();
        }

        public void Dispose() {
            _container.Dispose();
        }

        private void CreateInstances() {
            MappingExtensions.Mapper = new MapperConfiguration(cfg => {
                cfg.AddProfile<ArmaServerProfile>();
            }).CreateMapper();
            App.SteamHelper = _container.GetInstance<ISteamHelper>();
            LockedWrapper.callFactory = _container.GetInstance<ISafeCallFactory>();
                // workaround for accessviolation errors
            Cheat.SetServices(_container.GetInstance<ICheatImpl>());
            Raiser.Raiserr = new EventStorage();
            RequestScopeService.Instance = new RequestScopeService(_container);
        }

        public IEnumerable<BaseCommand> GetCommands() {
            yield return _container.GetInstance<InstallCommand>();
            yield return _container.GetInstance<UninstallCommand>();
            yield return _container.GetInstance<RunInteractive>();
        }

        // TODO: Use Assembly Discovery..
        void SetupContainer(Func<ISteamApi> steamApi) {
            _assemblies = new[] {Assembly.GetExecutingAssembly()};
            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new ExecutionContextScopeLifestyle();
            _container.RegisterSingleton<ICheatImpl, CheatImpl>();
            _container.RegisterSingleton<IExceptionHandler, UnhandledExceptionHandler>();
            _container.RegisterSingleton<IActionDispatcher>(
                () => new ActionDispatcher(_container.GetInstance<IMediator>(), null));
            BootstrapperBridge.RegisterMessageBus(_container);
            _container.RegisterSingleton<IMessageBusProxy, MessageBusProxy>();
            _container.RegisterSingleton<ISteamHelper>(SteamHelper.Create());
            _container.RegisterPlugins<IHandleExceptionPlugin>(_assemblies, Lifestyle.Singleton);
            _container.RegisterSingleton<SteamSession.SteamSessionFactory>();
            _container.RegisterSingleton<ISteamSessionFactory>(_container.GetInstance<SteamSession.SteamSessionFactory>);
            _container.RegisterSingleton<ISteamSessionLocator>(_container.GetInstance<SteamSession.SteamSessionFactory>);
            _container.RegisterSingleton<IServiceMessenger, ServiceMessenger>();
            _container.RegisterSingleton<ISteamDownloader, SteamDownloader>();
            _container.RegisterSingleton<Api.Services.ISteamApi, SteamApi>();
            _container.RegisterSingleton(steamApi);
            _container.RegisterSingleton<IEventStorage, EventStorage>();
            _container.RegisterSingleton<ISafeCallFactory, SafeCallFactory>();

            _container.RegisterValidation(_assemblies);
            _container.RegisterMediator(_assemblies);

            _container.RegisterDecorator<IMediator, MediatorLoggingDecorator>();
            _container.Register<IRequestScope, RequestScope>(Lifestyle.Scoped);
            _container.RegisterSingleton<IRequestScopeLocator, RequestScopeService>();
        }
    }
}