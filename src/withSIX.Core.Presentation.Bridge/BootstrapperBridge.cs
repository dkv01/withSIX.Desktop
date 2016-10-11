﻿// <copyright company="SIX Networks GmbH" file="BootstrapperBridge.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using MediatR;
using SimpleInjector;
using withSIX.Core.Presentation.Bridge.Extensions;
using withSIX.Core.Presentation.Decorators;

namespace withSIX.Core.Presentation.Bridge
{
    public class BootstrapperBridge
    {
        public static void LowInitializer() {
            PathConfiguration.GetFolderPath = new LowInitializer().GetFolderPath;
        }

        public static Assembly AssemblyLoadFrom(string arg) => Assembly.LoadFrom(arg);

        public static void RegisterMediatorDecorators(Container container) {
            container.RegisterDecorator<IMediator, MediatorValidationDecorator>(Lifestyle.Singleton);
            if (Common.AppCommon.Type < ReleaseType.Beta)
                container.RegisterDecorator<IMediator, MediatorLoggingDecorator>(Lifestyle.Singleton);
        }

        public static void ConfigureContainer(Container container) {
            // TODO: Disable this once we could disable registering inherited interfaces??
            container.Options.LifestyleSelectionBehavior = new CustomLifestyleSelectionBehavior();
            container.Options.AllowOverridingRegistrations = true;
            container.AllowResolvingFuncFactories();
            container.AllowResolvingLazyFactories();
        }

        public static void EnvironmentExit(int exitCode) => Environment.Exit(exitCode);

        public static void RegisterMessageBus(Container container) => container.RegisterMessageBus();

        /*
        public static IEnumerable<Type> GetTypes<T>(IEnumerable<Assembly> assemblies) => assemblies.GetTypes<T>();
                 public void RegisterPlugins<T>(Container container, IEnumerable<Assembly> assemblies, Lifestyle style = null)
            where T : class => container.RegisterPlugins<T>(assemblies, style);

        public void RegisterAllInterfaces<T>(Container container, IEnumerable<Assembly> assemblies)
            => container.RegisterAllInterfaces<T>(assemblies);

        public void RegisterSingleAllInterfaces<T>(Container container, IEnumerable<Assembly> assemblies)
            => container.RegisterSingleAllInterfaces<T>(assemblies);

         */
    }
}