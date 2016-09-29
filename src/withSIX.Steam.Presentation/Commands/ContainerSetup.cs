// <copyright company="SIX Networks GmbH" file="ContainerSetup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using MediatR;
using ReactiveUI;
using SimpleInjector;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Presentation.Bridge.Extensions;
using withSIX.Mini.Applications;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Commands;
using withSIX.Steam.Api;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Core;
using withSIX.Steam.Plugin.Arma;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;
using SteamApi = withSIX.Steam.Api.Services.SteamApi;

namespace withSIX.Steam.Presentation.Commands
{
    public class ContainerSetup : IDisposable
    {
        private IReadOnlyCollection<Assembly> _assemblies;
        private Container _container;

        public ContainerSetup(Func<ISteamApi> steamApi) {
            App.SteamHelper = SteamHelper.Create();
            LockedWrapper.callFactory = new SafeCallFactory(); // workaround for accessviolation errors
            SetupContainer(steamApi);
            CreateInstances();
        }

        public void Dispose() {
            _container.Dispose();
        }

        private void CreateInstances() {
            Cheat.SetServices(_container.GetInstance<ICheatImpl>());
            Raiser.Raiserr = new EventStorage();
        }

        public IEnumerable<BaseCommand> GetCommands() {
            yield return _container.GetInstance<InstallCommand>();
            yield return _container.GetInstance<UninstallCommand>();
            yield return _container.GetInstance<RunInteractive>();
        }

        void SetupContainer(Func<ISteamApi> steamApi) {
            _assemblies = new[] {Assembly.GetExecutingAssembly()};
            _container = new Container();
            _container.RegisterSingleton(new SingleInstanceFactory(_container.GetInstance));
            _container.RegisterSingleton(new MultiInstanceFactory(_container.GetAllInstances));
            _container.RegisterSingleton<ICheatImpl, CheatImpl>();
            _container.RegisterSingleton<IMediator, Mediator>();
            _container.RegisterSingleton<IExceptionHandler, UnhandledExceptionHandler>();
            _container.RegisterSingleton<IActionDispatcher>(
                () => new ActionDispatcher(_container.GetInstance<IMediator>(), null));
            _container.RegisterSingleton<IMessageBus, MessageBus>();
            _container.RegisterSingleton<ISteamHelper>(App.SteamHelper);
            _container.RegisterPlugins<IHandleExceptionPlugin>(_assemblies, Lifestyle.Singleton);
            _container.RegisterSingleton<ISteamSessionFactory, SteamSession.SteamSessionFactory>();
            _container.RegisterSingleton<ISteamSessionLocator, SteamSession.SteamSessionFactory>();
            _container.RegisterSingleton<ISteamDownloader, SteamDownloader>();
            _container.RegisterSingleton<Api.Services.ISteamApi, SteamApi>();
            _container.RegisterSingleton(steamApi);
            _container.RegisterSingleton<IEventStorage, EventStorage>();
            RegisterRequestHandlers();
            RegisterNotificationHandlers();
        }

        private void RegisterRequestHandlers() {
            var requestHandlers = new[] {
                typeof(IAsyncRequestHandler<,>),
                typeof(IRequestHandler<,>)
            };

            foreach (var h in requestHandlers)
                _container.Register(h, _assemblies, Lifestyle.Singleton);
        }

        private void RegisterNotificationHandlers() {
            var notificationHandlers = new[] {
                typeof(INotificationHandler<>),
                typeof(IAsyncNotificationHandler<>)
            };

            foreach (var h in notificationHandlers) {
                //_container.Register(h, _assemblies, Lifestyle.Singleton);
                //_container.RegisterSingleAllInterfacesAndType<>();
                _container.RegisterCollection(h, _assemblies);
            }
        }
    }
}