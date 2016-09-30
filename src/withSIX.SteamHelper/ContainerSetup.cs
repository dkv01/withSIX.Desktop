﻿// <copyright company="SIX Networks GmbH" file="ContainerSetup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using SimpleInjector;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Bridge.Extensions;
using withSIX.Core.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Commands;
using withSIX.Steam.Api;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Core;
using withSIX.Steam.Plugin.Arma;
using withSIX.Steam.Presentation.Commands;
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
            App.SteamHelper = _container.GetInstance<ISteamHelper>();
            LockedWrapper.callFactory = _container.GetInstance<ISafeCallFactory>(); // workaround for accessviolation errors
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
            BootstrapperBridge.RegisterMessageBus(_container);
            _container.RegisterSingleton<ISteamHelper>(SteamHelper.Create());
            _container.RegisterPlugins<IHandleExceptionPlugin>(_assemblies, Lifestyle.Singleton);
            _container.RegisterSingleton<ISteamSessionFactory, SteamSession.SteamSessionFactory>();
            _container.RegisterSingleton<ISteamSessionLocator, SteamSession.SteamSessionFactory>();
            _container.RegisterSingleton<ISteamDownloader, SteamDownloader>();
            _container.RegisterSingleton<Api.Services.ISteamApi, SteamApi>();
            _container.RegisterSingleton(steamApi);
            _container.RegisterSingleton<IEventStorage, EventStorage>();
            _container.RegisterSingleton<ISafeCallFactory, SafeCallFactory>();
            _container.RegisterDecorator<IMediator, MediatorLoggingDecorator>();
            RegisterRequestHandlers();
            RegisterNotificationHandlers();
        }

        private void RegisterRequestHandlers() {
            var requestHandlers = new[] {
                typeof(IAsyncRequestHandler<,>),
                typeof(ICancellableAsyncRequestHandler<,>),
                typeof(IRequestHandler<,>)
            };

            foreach (var h in requestHandlers)
                _container.Register(h, _assemblies, Lifestyle.Singleton);
        }

        private void RegisterNotificationHandlers() {
            var notificationHandlers = new[] {
                typeof(INotificationHandler<>),
                typeof(IAsyncNotificationHandler<>),
                typeof(ICancellableAsyncNotificationHandler<>)
            };

            foreach (var h in notificationHandlers) {
                //_container.Register(h, _assemblies, Lifestyle.Singleton);
                //_container.RegisterSingleAllInterfacesAndType<>();
                _container.RegisterCollection(h, _assemblies);
            }
        }
    }

    public class MediatorLoggingDecorator : MediatorDecoratorBase, IMediator
    {
        protected static readonly JsonSerializerSettings JsonSerializerSettings = CreateJsonSerializerSettings();

        public MediatorLoggingDecorator(IMediator decorated) : base(decorated) { }

        public override TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            using (
                Decorated.Bench(
                    startMessage:
                    "Writes: " + (request is IWrite) + ", Data: " +
                    JsonConvert.SerializeObject(request, JsonSerializerSettings),
                    caller: "Request" + ": " + request.GetType()))
                return base.Send(request);
        }

        public override async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            using (Decorated.Bench(
                startMessage:
                "Writes: " + (request is IWrite) + ", Data: " +
                JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                return await base.SendAsync(request).ConfigureAwait(false);
        }

        public override async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            using (Decorated.Bench(
                startMessage:
                "Writes: " + (request is IWrite) + ", Data: " +
                JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        static JsonSerializerSettings CreateJsonSerializerSettings() {
            var settings = new JsonSerializerSettings().SetDefaultSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return settings;
        }
    }
}