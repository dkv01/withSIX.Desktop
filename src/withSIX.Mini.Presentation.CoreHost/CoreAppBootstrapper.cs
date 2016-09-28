using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SimpleInjector;
using SN.withSIX.Core;
using SN.withSIX.Mini.Presentation.Core;

namespace withSIX.Mini.Presentation.CoreHost
{
    public abstract class WorkaroundBootstrapper : AppBootstrapper
    {
        protected override void EnvironmentExit(int exitCode) {
            Environment.Exit(exitCode);
        }

        /*
        protected WorkaroundBootstrapper(IMutableDependencyResolver dependencyResolver, string[] args)
            : base(dependencyResolver, args) { }

        protected override void LowInitializer() {
            PathConfiguration.GetFolderPath = new LowInitializer().GetFolderPath;
        }

        protected override IEnumerable<Assembly> GetInfraAssemblies
            => new[] { typeof(AutoMapperInfraApiConfig).GetTypeInfo().Assembly }.Concat(base.GetInfraAssemblies);

        protected override Assembly AssemblyLoadFrom(string arg) => Assembly.LoadFrom(arg);

        protected override void ConfigureContainer() {
            // TODO: Disable this once we could disable registering inherited interfaces??
            Container.Options.LifestyleSelectionBehavior = new CustomLifestyleSelectionBehavior();
            Container.Options.AllowOverridingRegistrations = true;
            Container.AllowResolvingFuncFactories();
            Container.AllowResolvingLazyFactories();
        }

        protected override void RegisterMessageBus() {
            Container.RegisterSingleton(new MessageBus());
            Container.RegisterSingleton<IMessageBus>(Container.GetInstance<MessageBus>);
        }

        protected override void RegisterPlugins<T>(IEnumerable<Assembly> assemblies, Lifestyle style = null)
            => Container.RegisterPlugins<T>(assemblies);

        protected override void RegisterAllInterfaces<T>(IEnumerable<Assembly> assemblies)
            => Container.RegisterAllInterfaces<T>(assemblies);

        protected override void RegisterSingleAllInterfaces<T>(IEnumerable<Assembly> assemblies)
            => Container.RegisterSingleAllInterfaces<T>(assemblies);

        protected override IEnumerable<Type> GetTypes<T>(IEnumerable<Assembly> assemblies) => assemblies.GetTypes<T>();
        */
        protected WorkaroundBootstrapper(string[] args) : base(args) {}
    }

    public abstract class CoreAppBootstrapper : WorkaroundBootstrapper
    {
        public CoreAppBootstrapper(string[] args) : base(args) {}
    }
}
