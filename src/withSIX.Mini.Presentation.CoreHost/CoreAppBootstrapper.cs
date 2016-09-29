// <copyright company="SIX Networks GmbH" file="CoreAppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using MediatR;
using NDepend.Path;
using SimpleInjector;
using SimpleInjector.Advanced;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Decorators;
using SN.withSIX.Mini.Presentation.Core;
using SN.withSIX.Mini.Presentation.Owin.Core;
using withSIX.Mini.Presentation.CoreCore.Services;
using SystemExtensions = withSIX.Api.Models.Extensions.SystemExtensions;

namespace withSIX.Mini.Presentation.CoreHost
{
    public abstract class WorkaroundBootstrapper : AppBootstrapper
    {
        private readonly IAbsoluteDirectoryPath _rootPath;
        readonly AssemblyLoader asl;

        protected WorkaroundBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath) : base(args, rootPath) {
            _rootPath = rootPath;
            asl = new AssemblyLoader(_rootPath.ToString());
        }

        // We dont actually want to load the Infra.Api as it's non .NET core atm
        //protected override IEnumerable<Assembly> GetInfraAssemblies
          //  => new[] {AssemblyLoadFrom(_rootPath.GetChildFileWithName("SN.withSIX.Mini.Infra.Api.dll").ToString())}.Concat(base.GetInfraAssemblies);

        protected override void EnvironmentExit(int exitCode) {
            Environment.Exit(exitCode);
        }

        protected override IEnumerable<Assembly> GetPresentationAssemblies() => new[] {Assembly.GetEntryAssembly(), typeof(AssemblyService).GetTypeInfo().Assembly}.Concat(base.GetPresentationAssemblies());

        protected override void ConfigureContainer() {
            // TODO: Disable this once we could disable registering inherited interfaces??
            Container.Options.LifestyleSelectionBehavior = new CustomLifestyleSelectionBehavior();
            Container.Options.AllowOverridingRegistrations = true;
            Container.AllowResolvingFuncFactories();
            Container.AllowResolvingLazyFactories();
        }

        protected override void ConfigureInstances() {
            base.ConfigureInstances();
            AssemblyService.AllAssemblies = GetAllAssemblies().ToArray();
        }

        protected override Assembly AssemblyLoadFrom(string arg) {
            var absoluteFilePath = arg.ToAbsoluteFilePath();
            var fileNameWithoutExtension = absoluteFilePath.FileNameWithoutExtension;
            var assemblyName = new AssemblyName(fileNameWithoutExtension);
            var a = asl;
            return a.LoadFromAssemblyName(assemblyName);
        }

        protected override void LowInitializer() {
            //new LowInitializer().GetFolderPath;
            PathConfiguration.GetFolderPath =
                folder =>
                    _rootPath.GetChildDirectoryWithName("Profile")
                        .GetChildDirectoryWithName(folder.ToString())
                        .ToString();
        }

        protected override void RegisterMessageBus() {
            //var l = Locator.CurrentMutable;
            // cant refer ReactiveUI atm until we put it into a package :)
            //l.Register(() => null, typeof(IFilesystemProvider), null);
            SN.withSIX.Core.Presentation.AppBootstrapper.RegisterMessageBus(Container);
        }



        protected override void RegisterPlugins<T>(IEnumerable<Assembly> assemblies, Lifestyle style = null)
            => Container.RegisterPlugins<T>(assemblies);

        protected override void RegisterAllInterfaces<T>(IEnumerable<Assembly> assemblies)
            => Container.RegisterAllInterfaces<T>(assemblies);

        protected override void RegisterSingleAllInterfaces<T>(IEnumerable<Assembly> assemblies)
            => Container.RegisterSingleAllInterfaces<T>(assemblies);

        protected override IEnumerable<Type> GetTypes<T>(IEnumerable<Assembly> assemblies) => assemblies.GetTypes<T>();

        public class AssemblyLoader : AssemblyLoadContext
        {
            private readonly string folderPath;

            public AssemblyLoader(string folderPath) {
                this.folderPath = folderPath;
            }

            protected override Assembly Load(AssemblyName assemblyName) {
                //var deps = DependencyContext.Default;
                //var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
                //if (res.Count > 0) {
                //return Assembly.Load(new AssemblyName(res.First().Name));
                //} else {
                var apiApplicationFileInfo =
                    new FileInfo($"{folderPath}{Path.DirectorySeparatorChar}{assemblyName.Name}.dll");
                if (File.Exists(apiApplicationFileInfo.FullName)) {
                    var asl = new AssemblyLoader(apiApplicationFileInfo.DirectoryName);
                    return asl.LoadFromAssemblyPath(apiApplicationFileInfo.FullName);
                }
                //}
                return Assembly.Load(assemblyName);
            }
        }

        public override void Configure() {
            base.Configure();
            // throws atm, we need updated Splat/RXUI :-)
            //Locator.CurrentMutable.Register(() => new JsonSerializerSettings().SetDefaultConverters(), typeof(JsonSerializerSettings));
            Container.RegisterPlugins<OwinModule>(GetInfraAssemblies);
        }
    }

    public class CoreAppBootstrapper : WorkaroundBootstrapper
    {
        public CoreAppBootstrapper(string[] args, IAbsoluteDirectoryPath rootPath) : base(args, rootPath) {}
    }

    public class CustomLifestyleSelectionBehavior : ILifestyleSelectionBehavior
    {
        readonly Type[] _notificationHandlers = {
            typeof(INotificationHandler<>),
            typeof(IAsyncNotificationHandler<>),
            typeof(ICancellableAsyncNotificationHandler<>)
        };
        readonly Lifestyle lifestyle = Lifestyle.Transient;

        public Lifestyle SelectLifestyle(Type serviceType, Type implementationType) {
            //Requires.IsNotNull(serviceType, nameof(serviceType));
            //Requires.IsNotNull(implementationType, nameof(implementationType));
            if (serviceType.GetTypeInfo().IsGenericType) {
                if (_notificationHandlers.Contains(serviceType.GetGenericTypeDefinition()))
                    return Lifestyle.Singleton;
            }

            return lifestyle;
        }
    }

    public static class SimpleInjectorContainerExtensions
    {
        static readonly List<Type> reserved = new List<Type> {
            typeof(INotifyPropertyChanged),
            typeof(INotifyPropertyChanging),
            typeof(IEnableLogging),
            typeof(IDisposable)
        };
        static readonly List<Type> reservedRoot = new List<Type>();

        public static void RegisterMediatorDecorators(Container container) {
            container.RegisterDecorator<IMediator, MediatorValidationDecorator>(Lifestyle.Singleton);
            if (Common.AppCommon.Type < ReleaseType.Beta)
                container.RegisterDecorator<IMediator, MediatorLoggingDecorator>(Lifestyle.Singleton);
        }


        public static void RegisterReserved(params Type[] ts) => reserved.AddRange(ts);
        public static void RegisterReservedRoot(params Type[] ts) => reservedRoot.AddRange(ts);

        public static void RegisterPlugins<T>(this Container container, IEnumerable<Assembly> assemblies,
            Lifestyle lifestyle = null) where T : class {
            var pluginTypes = GetTypes<T>(assemblies).ToArray();
            HandleLifestyle<T>(container, lifestyle, pluginTypes);
            container.RegisterCollection<T>(pluginTypes);
        }

        static void HandleLifestyle<T>(Container container, Lifestyle lifestyle, IEnumerable<Type> pluginTypes)
            where T : class {
            if ((lifestyle == null) || (lifestyle == Lifestyle.Transient))
                return;
            foreach (var t in pluginTypes)
                container.Register(t, t, lifestyle);
        }

        public static void RegisterPlugins<TImplements, TExport>(this Container container,
            IEnumerable<Assembly> assemblies, Lifestyle lifestyle = null) where TExport : class {
            var pluginTypes = GetTypes<TImplements>(assemblies).ToArray();
            HandleLifestyle<TExport>(container, lifestyle, pluginTypes);
            container.RegisterCollection<TExport>(pluginTypes);
        }

        public static IEnumerable<Type> GetTypes<T>(this IEnumerable<Assembly> assemblies)
            => assemblies.GetTypes(typeof(T));

        public static IEnumerable<Type> GetTypes(this IEnumerable<Assembly> assemblies, Type t)
            =>
            assemblies.Distinct()
                .SelectMany(assembly => {
                        Type[] types;
                        try {
                            types = assembly.GetTypes();
                        } catch (ReflectionTypeLoadException e) {
                            types = e.Types;
                        }
                    return types.Where(t2 => t2 != null);
                }, (assembly, type) => new {assembly, type, ti = type.GetTypeInfo()})
                .Where(t1 => SystemExtensions.IsAssignableFrom(t, t1.type))
                .Where(t1 => !t1.ti.IsAbstract)
                .Where(t1 => !t1.ti.IsGenericTypeDefinition)
                .Where(t1 => !t1.ti.IsInterface)
                .Select(t1 => t1.type);

        public static void RegisterAllInterfaces<T>(this Container container, IEnumerable<Assembly> assemblies) {
            var ifaceType = typeof(T);
            foreach (var s in assemblies.GetTypes<T>()) {
                foreach (var i in s.GetInterfaces().Where(x => Predicate(x, ifaceType)))
                    container.Register(i, s);
            }
        }

        static bool Predicate(Type x, Type ifaceType)
            =>
            (x != ifaceType) && !reserved.Contains(x) && !reservedRoot.Any(t => SystemExtensions.IsAssignableFrom(t, x));

        public static void RegisterAllInterfacesAndType<T>(this Container container,
            IEnumerable<Assembly> assemblies, Func<Type, bool> predicate = null) {
            var enumerable = assemblies.GetTypes<T>();
            if (predicate != null)
                enumerable = enumerable.Where(predicate);
            RegisterInterfacesAndType<T>(container, enumerable);
        }

        public static void RegisterInterfacesAndType<T>(this Container container, IEnumerable<Type> types) {
            var ifaceType = typeof(T);
            foreach (var s in types) {
                container.Register(s, s);
                foreach (var i in s.GetInterfaces().Where(x => Predicate(x, ifaceType)))
                    container.Register(i, s);
            }
        }

        public static void RegisterInterfaces<T>(this Container container, IEnumerable<Type> types) {
            var ifaceType = typeof(T);
            foreach (var s in types) {
                foreach (var i in s.GetInterfaces().Where(x => Predicate(x, ifaceType)))
                    container.Register(i, s);
            }
        }

        public static void RegisterSingleInterfacesAndType<T>(this Container container, IEnumerable<Type> types) {
            var ifaceType = typeof(T);
            foreach (var s in types) {
                container.RegisterSingleton(s, s);
                foreach (var i in s.GetInterfaces().Where(x => Predicate(x, ifaceType)))
                    container.RegisterSingleton(i, () => container.GetInstance(s));
            }
        }

        public static void RegisterSingleInterfaces<T>(this Container container, IEnumerable<Type> types) {
            var ifaceType = typeof(T);
            foreach (var s in types) {
                foreach (var i in s.GetInterfaces().Where(x => Predicate(x, ifaceType)))
                    container.RegisterSingleton(i, s);
            }
        }

        public static void RegisterSingleAllInterfaces<T>(this Container container, IEnumerable<Assembly> assemblies,
            Func<Type, bool> predicate = null) {
            var enumerable = assemblies.GetTypes<T>();
            if (predicate != null)
                enumerable = enumerable.Where(predicate);
            RegisterSingleInterfaces<T>(container, enumerable);
        }

        public static void RegisterSingleAllInterfacesAndType<T>(this Container container,
            IEnumerable<Assembly> assemblies, Func<Type, bool> predicate = null) {
            var enumerable = assemblies.GetTypes<T>();
            if (predicate != null)
                enumerable = enumerable.Where(predicate);
            RegisterSingleInterfacesAndType<T>(container, enumerable);
        }

        public static void RegisterLazy(this Container container, Type serviceType) {
            var method = typeof(SimpleInjectorContainerExtensions).GetMethod("CreateLazy")
                .MakeGenericMethod(serviceType);

            var lazyInstanceCreator =
                Expression.Lambda<Func<object>>(
                        Expression.Call(method, Expression.Constant(container)))
                    .Compile();

            var lazyServiceType =
                typeof(Lazy<>).MakeGenericType(serviceType);

            container.Register(lazyServiceType, lazyInstanceCreator);
        }

        public static void RegisterLazy<T>(this Container container) where T : class {
            Func<T> factory = container.GetInstance<T>;
            container.Register(() => new Lazy<T>(factory));
        }
    }

    public static class ResolvingFactoriesExtensions
    {
        /*
                 public static void AllowResolvingExportFactories(this Container container) {
            container.ResolveUnregisteredType += (s, e) => {
                var type = e.UnregisteredServiceType;
                if (!type.IsGenericType ||
                    type.GetGenericTypeDefinition() != typeof (ExportFactory<>))
                    return;

                var args = type.GetGenericArguments();
                var method = typeof (ResolvingFactoriesExtensions).GetMethod("CreateEF")
                    .MakeGenericMethod(args[0]);
                var factoryDelegate = Expression.Lambda<Func<object>>(Expression.Call(method,
                    Expression.Constant(container))).Compile()();
                e.Register(Expression.Constant(factoryDelegate));
            };
        }

        public static ExportFactory<T> CreateEF<T>(Container container) where T : class {
            ConfirmTransient(container, typeof (T));
            return new ExportFactory<T>(() => {
                var instance = new Lazy<T>(container.GetInstance<T>);
                var act = typeof (IDisposable).IsAssignableFrom(typeof (T))
                    ? () => {
                        if (instance.IsValueCreated)
                            ((IDisposable) instance.Value).Dispose();
                    }
                    : TaskExt.NullAction;
                return new Tuple<T, Action>(instance.Value, act);
            });
        }*/

        static void ConfirmTransient(Container container, Type serviceType) {
            var registration = container.GetRegistration(serviceType);
            ConfirmTransient(registration, serviceType);
        }

        static void ConfirmTransient(InstanceProducer registration, Type serviceType) {
            if (registration.Lifestyle != Lifestyle.Transient)
                throw new ArgumentOutOfRangeException("type", "Lifestyle != LifeStyle.Transient for: " + serviceType);
        }

        // This extension method is equivalent to the following registration, for each and every T:
        // container.RegisterSingleton<Func<T>>(() => container.GetInstance<T>());
        // This is useful for consumers that need to create multiple instances of a dependency.
        // This mimics the behavior of Autofac. In Autofac this behavior is default.
        public static void AllowResolvingFuncFactories(this Container container) {
            container.ResolveUnregisteredType += (sender, e) => {
                if (!e.UnregisteredServiceType.GetTypeInfo().IsGenericType ||
                    (e.UnregisteredServiceType.GetGenericTypeDefinition() != typeof(Func<>)))
                    return;
                var serviceType = e.UnregisteredServiceType.GetGenericArguments()[0];

                var registration = container.GetRegistration(serviceType, true);
                ConfirmTransient(registration, serviceType);

                var funcType = typeof(Func<>).MakeGenericType(serviceType);

                var factoryDelegate =
                    Expression.Lambda(funcType, registration.BuildExpression()).Compile();

                e.Register(Expression.Constant(factoryDelegate));
            };
        }

        // This extension method is equivalent to the following registration, for each and every T:
        // container.Register<Lazy<T>>(() => new Lazy<T>(() => container.GetInstance<T>()));
        // This is useful for consumers that have a dependency on a service that is expensive to create, but
        // not always needed.
        // This mimics the behavior of Autofac and Ninject 3. In Autofac this behavior is default.
        public static void AllowResolvingLazyFactories(this Container container) {
            container.ResolveUnregisteredType += (sender, e) => {
                if (!e.UnregisteredServiceType.GetTypeInfo().IsGenericType ||
                    (e.UnregisteredServiceType.GetGenericTypeDefinition() != typeof(Lazy<>)))
                    return;
                var serviceType = e.UnregisteredServiceType.GetGenericArguments()[0];


                var method = ReflectionExtensions.GetGeneric<Func<Container, Lazy<object>>>(CreateLazy<object>,
                    serviceType);

                var factoryDelegate =
                    Expression.Lambda<Func<object>>(Expression.Call(method, Expression.Constant(container)))
                        .Compile()();

                e.Register(Expression.Constant(factoryDelegate));

                /*
                var registration = container.GetRegistration(serviceType, true);

                var funcType = typeof (Func<>).MakeGenericType(serviceType);
                var lazyType = typeof (Lazy<>).MakeGenericType(serviceType);

                var factoryDelegate =
                    Expression.Lambda(funcType, registration.BuildExpression()).Compile();

                var lazyConstructor = (
                    from ctor in lazyType.GetConstructors()
                    where ctor.GetParameters().Length == 1
                    where ctor.GetParameters()[0].ParameterType == funcType
                    select ctor)
                    .Single();

                e.Register(Expression.New(lazyConstructor, Expression.Constant(factoryDelegate)));
                */
            };
        }


        public static Lazy<T> CreateLazy<T>(Container container)
            where T : class => new Lazy<T>(container.GetInstance<T>);

        public static void AllowResolvingParameterizedFuncFactories(this Container container) {
            container.ResolveUnregisteredType += (sender, e) => {
                if (!IsParameterizedFuncDelegate(e.UnregisteredServiceType))
                    return;

                var genericArguments = e.UnregisteredServiceType.GetGenericArguments();

                var componentType = genericArguments.Last();

                if (componentType.GetTypeInfo().IsAbstract)
                    return;

                var funcType = e.UnregisteredServiceType;

                var factoryArguments = genericArguments.Take(genericArguments.Length - 1).ToArray();

                var serviceType = factoryArguments.Last();
                ConfirmTransient(container, serviceType);

                var constructor = container.Options.ConstructorResolutionBehavior
                    .GetConstructor(componentType, componentType);

                //var ctorArguments = constructor.GetParameters().Select(p => p.ParameterType).ToArray();

                var parameters = (
                        from factoryArgumentType in factoryArguments
                        select Expression.Parameter(factoryArgumentType))
                    .ToArray();

                var factoryDelegate = Expression.Lambda(funcType,
                        BuildNewExpression(container, constructor, parameters),
                        parameters)
                    .Compile();

                e.Register(Expression.Constant(factoryDelegate));
            };
        }

        static bool IsParameterizedFuncDelegate(Type type) {
            var ti = type.GetTypeInfo();
            if (!ti.IsGenericType || !type.FullName.StartsWith("System.Func`"))
                return false;

            return type.GetGenericTypeDefinition().GetGenericArguments().Length > 1;
        }

        static NewExpression BuildNewExpression(Container container,
            ConstructorInfo constructor,
            ParameterExpression[] funcParameterExpression) {
            var ctorParameters = constructor.GetParameters();
            var ctorParameterTypes = ctorParameters.Select(p => p.ParameterType).ToArray();
            var funcParameterTypes = funcParameterExpression.Select(p => p.Type).ToArray();

            var funcParametersIndex = IndexOfSubCollection(ctorParameterTypes, funcParameterTypes);

            if (funcParametersIndex == -1) {
                throw new ActivationException(
                    $"The constructor of type {constructor.DeclaringType.FullName} did not contain the sequence of the following " +
                    $"constructor parameters: {string.Join(", ", funcParameterTypes.Select(t => t.Name))}.");
            }

            var firstCtorParameterExpressions = ctorParameterTypes
                .Take(funcParametersIndex)
                .Select(type => container.GetRegistration(type, true).BuildExpression());

            var lastCtorParameterExpressions = ctorParameterTypes
                .Skip(funcParametersIndex + funcParameterTypes.Length)
                .Select(type => container.GetRegistration(type, true).BuildExpression());

            var expressions = firstCtorParameterExpressions.Concat(funcParameterExpression)
                .Concat(lastCtorParameterExpressions)
                .ToArray();

            return Expression.New(constructor, expressions);
        }

        static int IndexOfSubCollection(Type[] collection, Type[] subCollection)
            => (
                       from index in
                       Enumerable.Range(0,
                           collection.Length -
                           subCollection.Length + 1)
                       let collectionPart =
                       collection.Skip(index)
                           .Take(
                               subCollection.Length)
                       where
                       collectionPart.SequenceEqual
                           (subCollection)
                       select (int?) index)
                   .FirstOrDefault() ?? -1;
    }
}