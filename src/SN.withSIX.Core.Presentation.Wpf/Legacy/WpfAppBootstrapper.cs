// <copyright company="SIX Networks GmbH" file="WpfAppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using ReactiveUI;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.MVVM.Services;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Core.Applications.MVVM.ViewModels.Dialogs;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Bridge.Extensions;
using SN.withSIX.Core.Presentation.Bridge.Services;
using SN.withSIX.Core.Presentation.Resources;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Core.Presentation.Wpf.Extensions;
using SN.withSIX.Core.Presentation.Wpf.Services;
using Splat;
using IScreen = Caliburn.Micro.IScreen;
using ViewLocator = Caliburn.Micro.ViewLocator;

namespace SN.withSIX.Core.Presentation.Wpf.Legacy
{
    public abstract class WpfAppBootstrapper<T> : AppBootstrapperBase
    {
        static readonly Type idontic = typeof(IDontIC);
        static readonly Type[] _meepfaces = {typeof(IContextMenu), typeof(IDialog)};
        static readonly Type _transient = typeof(ITransient);
        static readonly Type _singleton = typeof(ISingleton);
        readonly DependencyObjectObservableForProperty workaroundForSmartAssemblyReactiveXAML;
        IExceptionHandler _exceptionHandler;
        protected bool DisplayRootView = true;

        protected override void Configure() {
            base.Configure();
            RxApp.SupportsRangeNotifications = false; // WPF doesnt :/
            SimpleInjectorContainerExtensions.RegisterReserved(typeof(IHandle), typeof(IScreen));
            SimpleInjectorContainerExtensions.RegisterReservedRoot(typeof(IHandle));
            UiTaskHandler.RegisterCommand = (command, action) => {
                // ThrownExceptions does not listen to Subscribe errors, but only in async task errors!
                command.ThrownExceptions
                    .Select(x => ErrorHandlerr.HandleException(x, action))
                    .SelectMany(UserErrorHandler.HandleUserError)
                    .Where(x => x == RecoveryOptionResultModel.RetryOperation)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .InvokeCommand(command);
            };
            SetupViewNamespaces();

            var mutableDependencyResolver = Locator.CurrentMutable;
            SetupRx(mutableDependencyResolver);
        }

        protected override IEnumerable<Assembly> SelectAssemblies() => new[] {
            typeof(WpfAppBootstrapper<>).Assembly,
            typeof(DummyClass).Assembly
        }.Concat(base.SelectAssemblies()).Distinct();

        protected virtual void SetupRx(IMutableDependencyResolver dependencyResolver) {
            var viewLocator = new DefaultViewLocator();
            dependencyResolver.Register(() => viewLocator, typeof(IViewLocator));
        }

        protected virtual void SetupViewNamespaces() {
            var original = ViewLocator.LocateForModel;
            ViewLocator.LocateForModel = (o, dependencyObject, arg3) => {
                var v = original(o, dependencyObject, arg3);
                // TODO: Lacks CM's Context/target support
                if ((v == null) || v is TextBlock) {
                    var rxv = (UIElement) ReactiveUI.ViewLocator.Current.ResolveView(o);
                    if (rxv != null)
                        v = rxv;
                }

                var vFor = v as IViewFor;
                if (vFor != null)
                    vFor.ViewModel = o;

                return v;
            };
            //ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Presentation.Wpf.Views", "SN.withSIX.Core.Presentation.Wpf.Views");
            ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Applications.MVVM.ViewModels.Popups",
                "SN.withSIX.Core.Presentation.Wpf.Views.Popups");
            ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Applications.MVVM.ViewModels.Dialogs",
                "SN.withSIX.Core.Presentation.Wpf.Views.Dialogs");
            ViewLocator.AddNamespaceMapping("SN.withSIX.Core.Applications.MVVM.ViewModels",
                "SN.withSIX.Core.Presentation.Wpf.Views");
        }

        protected virtual void PreStart() {}

        protected sealed override void OnStartup(object sender, StartupEventArgs e) {
            using (this.Bench()) {
                try {
                    SetupContainer();
                    SetupExceptionHandling();
                    using (MainLog.Bench(null, "WpfAppBootstrapper.PreStart"))
                        PreStart();
                    using (MainLog.Bench(null, "WpfAppBootstrapper.OnStartup"))
                        base.OnStartup(sender, e);
                    if (DisplayRootView)
                        DisplayRootViewFor<T>();
                } catch (Exception ex) {
                    LogError(ex, "Startup");
                    throw;
                }
            }
        }

        void SetupExceptionHandling() {
            _exceptionHandler = Container.GetInstance<IExceptionHandler>();
            ErrorHandlerr.SetExceptionHandler(_exceptionHandler);
            OverrideCaliburnMicroActionHandling();
        }

        void OverrideCaliburnMicroActionHandling() {
            var originalInvokeAction = ActionMessage.InvokeAction;
            ActionMessage.InvokeAction =
                actionExecutionContext => ExecuteAction(originalInvokeAction, actionExecutionContext);
        }

        bool ExecuteAction(Action<ActionExecutionContext> originalInvokeAction,
            ActionExecutionContext actionExecutionContext) {
            using (
                MainLog.Bench("UIAction",
                    actionExecutionContext.Target.GetType().Name + "." + actionExecutionContext.Method.Name)) {
                // TODO: This can deadlock ...
                // TODO: Don't use caliburn micro action handling, but use RXUI's task based Commands
                return
                    _exceptionHandler.TryExecuteAction(() => AsyncWrap(originalInvokeAction, actionExecutionContext))
                        .Result;
            }
        }

        static async Task AsyncWrap(Action<ActionExecutionContext> originalInvokeAction,
            ActionExecutionContext actionExecutionContext) {
            originalInvokeAction(actionExecutionContext);
        }

        protected sealed override void OnExit(object sender, EventArgs e) {
            try {
                base.OnExit(sender, e);
                ExitAsync(sender, e).WaitSpecial();
                Exit(sender, e);
            } catch (Exception ex) {
                LogError(ex, "Exit");
                throw;
            }
        }

        protected virtual void Exit(object sender, EventArgs eventArgs) {}
        protected virtual async Task ExitAsync(object sender, EventArgs eventArgs) {}

        static void LogError(Exception ex, string type) {
            try {
                MainLog.Logger.FormattedErrorException(ex, "Error during " + type);
            } catch {}
        }

        protected override void ConfigureContainer() {
            base.ConfigureContainer();
            Container.RegisterSingleton<IExitHandler, ExitHandler>();
            Container.RegisterSingleton<IShutdownHandler, WpfShutdownHandler>();
            Container.RegisterSingleton<IFirstTimeLicense, WpfFirstTimeLicense>();
            Container.RegisterSingleton<IDialogManager, WpfDialogManager>();
            Container.RegisterSingleton<IWindowManager, CustomWindowManager>();

            ExportViews();
            ExportViewModels();
        }

        void ExportViews() {
            Container.RegisterAllInterfacesAndType<UserControl>(AssemblySource.Instance, x => x.Name.EndsWith("View"));
            Container.RegisterAllInterfacesAndType<Window>(AssemblySource.Instance, x => x.Name.EndsWith("View"));
        }

        void ExportViewModels() {
            //Container.RegisterSingleAllInterfacesAndType<IShellViewModel>(AssemblySource.Instance,
            //x => !x.GetInterfaces().Contains(value));

            // nutters
            Container.RegisterSingleAllInterfacesAndType<ViewModelBase>(AssemblySource.Instance, Filter2);
            Container.RegisterSingleAllInterfacesAndType<ReactiveScreen>(AssemblySource.Instance, Filter2);
            Container.RegisterAllInterfacesAndType<ViewModelBase>(AssemblySource.Instance, Filter2Transient);
            Container.RegisterAllInterfacesAndType<ReactiveScreen>(AssemblySource.Instance, Filter2Transient);


            // Used by factories, should not be singleton!!
            Container.RegisterAllInterfacesAndType<IContextMenu>(AssemblySource.Instance, Filter);
            Container.RegisterAllInterfacesAndType<IDialog>(AssemblySource.Instance, Filter);
        }

        bool Filter(Type arg) => FilterInternal(arg, arg.GetInterfaces());

        bool Filter2Transient(Type arg) => Filter2TransientInternal(arg, arg.GetInterfaces());

        bool Filter2(Type arg) => Filter2Internal(arg, arg.GetInterfaces());

        // TODO: compose methods by passing the interfaces instead of getting over and over?
        static bool FilterInternal(Type type, Type[] interfaces) => !interfaces.Contains(idontic);

        static bool Filter2Internal(Type type, Type[] interfaces)
            => FilterInternal(type, interfaces) && MeepFilter(type, interfaces) &&
               IsSingleTonOverridenOrNotTransient(type, interfaces);

        static bool IsSingleTonOverridenOrNotTransient(Type type, Type[] interfaces)
            => interfaces.Contains(_singleton) || !interfaces.Contains(_transient);

        static bool Filter2TransientInternal(Type type, Type[] interfaces)
            => FilterInternal(type, interfaces) && MeepFilter(type, interfaces) &&
               IsTransientAndNotSingletonOverriden(type, interfaces);

        static bool IsTransientAndNotSingletonOverriden(Type type, Type[] interfaces)
            => interfaces.Contains(_transient) && !interfaces.Contains(_singleton);

        static bool MeepFilter(Type type, Type[] interfaces) => !_meepfaces.Any(interfaces.Contains);
    }
}