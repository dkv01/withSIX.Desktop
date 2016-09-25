// <copyright company="SIX Networks GmbH" file="RunInteractive.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Owin.Hosting;
using ReactiveUI;
using SimpleInjector;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Presentation.Extensions;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Plugin.Arma.Steam;
using SN.withSIX.Mini.Presentation.Core;
using SN.withSIX.Mini.Presentation.Core.Commands;
using SN.withSIX.Steam.Core;
using SteamLayerWrap;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Steam.Presentation.Commands
{
    public class ContainerSetup : IDisposable {
        private Container _container;
        private IReadOnlyCollection<Assembly> _assemblies;

        public ContainerSetup(ISteamApi steamApi) {
            SetupContainer(steamApi);
        }

        void SetupContainer(ISteamApi steamApi) {
            _assemblies = new[] { Assembly.GetExecutingAssembly() };
            _container = new Container();
            _container.RegisterSingleton(new SingleInstanceFactory(_container.GetInstance));
            _container.RegisterSingleton(new MultiInstanceFactory(_container.GetAllInstances));
            _container.RegisterSingleton<ICheatImpl, CheatImpl>();
            _container.RegisterSingleton<IMediator, Mediator>();
            _container.RegisterSingleton<IExceptionHandler, UnhandledExceptionHandler>();
            _container.RegisterSingleton<IActionDispatcher>(
                () => new ActionDispatcher(_container.GetInstance<IMediator>(), null));
            _container.RegisterSingleton<IMessageBus, MessageBus>();
            _container.RegisterSingleton<ISteamHelper>(SteamHelper.Create);
            _container.RegisterPlugins<IHandleExceptionPlugin>(_assemblies, Lifestyle.Singleton);
            _container.RegisterSingleton<ISteamApi>(steamApi);

            RegisterRequestHandlers();
            RegisterNotificationHandlers();
            Cheat.SetServices(_container.GetInstance<ICheatImpl>());
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

        public void Dispose() {
            _container.Dispose();
        }
    }

    public class RunInteractive : BaseCommandAsync
    {
        public RunInteractive() {
            IsCommand("interactive", "Run in interactive mode");
        }

        protected override async Task<int> RunAsync(string[] remainingArguments) {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            SteamAPIRunner.callFactory = new SafeCallFactory(); // workaround for accessviolation errors
            using (var wrap = new SteamAPIWrap()) {
                var steamApi = new SteamApi(wrap);
                using (new ContainerSetup(steamApi)) {
                    await
                        steamApi.Initialize(SteamHelper.Create().TryGetSteamAppById((uint) SteamGameIds.Arma3).AppPath)
                            .ConfigureAwait(false);
                    WebApp.Start<Startup>("http://127.0.0.66:48667");
                    Console.WriteLine("Ready");
                    await Task.Delay(-1).ConfigureAwait(false);
                    return 0;
                }
            }
        }

        private void CurrentDomainOnUnhandledException(object sender,
            UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
            Console.WriteLine(unhandledExceptionEventArgs.ExceptionObject);
        }
    }
}