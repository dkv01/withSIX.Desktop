// <copyright company="SIX Networks GmbH" file="ElectronAppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SimpleInjector;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Presentation.Core;
using Splat;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class ElectronAppBootstrapper : AppBootstrapper
    {
        private IDisposable _stateSetter;

        public ElectronAppBootstrapper(Container container, IMutableDependencyResolver dependencyResolver, string[] args)
            : base(container, dependencyResolver, args) {}

        protected override void Dispose(bool d) {
            base.Dispose(d);
            _stateSetter?.Dispose();
        }

        protected override IEnumerable<Assembly> GetPresentationAssemblies()
            => base.GetPresentationAssemblies().Concat(new[] {typeof (NodeApi).Assembly});

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