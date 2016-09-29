// <copyright company="SIX Networks GmbH" file="ServiceRegister.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SimpleInjector;
using withSIX.Mini.Applications.Services;

namespace withSIX.Mini.Presentation.Core.Services
{
    public class ServiceRegisterer : IRegisterServices
    {
        private readonly Container _container;

        public ServiceRegisterer(Container container) {
            _container = container;
        }

        public void Register<TService, TImplementation>() where TImplementation : class, TService where TService : class {
            _container.Register<TService, TImplementation>();
        }

        public void RegisterSingleton<TService, TImplementation>() where TImplementation : class, TService
            where TService : class {
            _container.RegisterSingleton<TService, TImplementation>();
        }
    }
}