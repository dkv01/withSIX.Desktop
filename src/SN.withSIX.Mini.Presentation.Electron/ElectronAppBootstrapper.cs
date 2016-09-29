// <copyright company="SIX Networks GmbH" file="ElectronAppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NDepend.Path;
using Newtonsoft.Json;
using ReactiveUI;
using SimpleInjector;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation.Bridge;
using SN.withSIX.Core.Presentation.Bridge.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Infra.Api;
using SN.withSIX.Mini.Presentation.Core;
using Splat;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Electron
{

    // These are here, in non .netstandard land because of ReactiveUI references, code contract issues.
    // We need RXUI7!
    public static class ErrorHandlerCheat
    {
        public static IStdErrorHandler Handler { get; set; }
    }

    public interface IStdErrorHandler
    {
        Task<RecoveryOptionResult> Handler(UserError error);
    }

    class WorkaroundBootstrapper : AppBootstrapper
    {
        protected WorkaroundBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath) : base(args, rootPath) { }

        protected override void LowInitializer() => BootstrapperBridge.LowInitializer();
        protected override void RegisterPlugins<T>(IEnumerable<Assembly> assemblies, Lifestyle style = null) => Container.RegisterPlugins<T>(assemblies);
        protected override IEnumerable<Type> GetTypes<T>(IEnumerable<Assembly> assemblies) => assemblies.GetTypes<T>();
        protected override void RegisterAllInterfaces<T>(IEnumerable<Assembly> assemblies) => Container.RegisterAllInterfaces<T>(assemblies);
        protected override void RegisterSingleAllInterfaces<T>(IEnumerable<Assembly> assemblies) => Container.RegisterSingleAllInterfaces<T>(assemblies);
        protected override IEnumerable<Assembly> GetInfraAssemblies => new[] { typeof(AutoMapperInfraApiConfig).GetTypeInfo().Assembly }.Concat(base.GetInfraAssemblies);
        protected override Assembly AssemblyLoadFrom(string arg) => BootstrapperBridge.AssemblyLoadFrom(arg);
        protected override void ConfigureContainer() => BootstrapperBridge.ConfigureContainer(Container);
        protected override void EnvironmentExit(int exitCode) => BootstrapperBridge.EnvironmentExit(exitCode);
        protected override void RegisterMessageBus() => BootstrapperBridge.RegisterMessageBus(Container);
    }

    class ElectronAppBootstrapper : WorkaroundBootstrapper
    {
        private IDisposable _stateSetter;

        protected override Task AfterUi() {
            if (ErrorHandlerCheat.Handler != null)
                UserError.RegisterHandler(error => ErrorHandlerCheat.Handler.Handler(error));
            return base.AfterUi();
        }

        public override void Configure() {
            base.Configure();
            Locator.CurrentMutable.Register(() => new JsonSerializerSettings().SetDefaultConverters(),
                typeof(JsonSerializerSettings));
        }

        public ElectronAppBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath)
            : base(args, rootPath) {}

        protected override void Dispose(bool d) {
            base.Dispose(d);
            _stateSetter?.Dispose();
        }

        protected override IEnumerable<Assembly> GetPresentationAssemblies()
            => base.GetPresentationAssemblies().Concat(new[] {typeof(NodeApi).Assembly});

        protected override void RegisterServices() {
            base.RegisterServices();
            Container.RegisterSingleton(() => Entrypoint.Api);
            Container.RegisterSingleton<IDialogManager, NodeDialogManager>();
            Container.RegisterSingleton<IShutdownHandler, ShutdownHandler>(); // TODO: better shutdown for node handler
        }

        protected override void BackgroundActions() {
            base.BackgroundActions();
            var stateHandler = Container.GetInstance<IStateHandler>();
            _stateSetter = stateHandler.StatusObservable //.ObserveOn(ThreadPoolScheduler.Instance)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ConcatTask(SetState)
                .Subscribe();
        }

        private Task SetState(ActionTabState x) =>
            Entrypoint.Api.SetState(x.Type == ActionType.Start ? BusyState.On : BusyState.Off,
                x.ChildAction.Details ?? x.Text, x.Progress);
    }
}