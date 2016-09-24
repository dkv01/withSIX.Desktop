// <copyright company="SIX Networks GmbH" file="ServiceRegistry.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Mini.Applications.Services
{
    public abstract class ServiceRegistry
    {
        private readonly IRegisterServices _api;

        protected ServiceRegistry(IRegisterServices api) {
            _api = api;
        }

        protected void Register<TService, TImplementation>() where TImplementation : class, TService
            where TService : class => _api.Register<TService, TImplementation>();

        protected void RegisterSingleton<TService, TImplementation>() where TImplementation : class, TService
            where TService : class => _api.RegisterSingleton<TService, TImplementation>();
    }

    public interface IRegisterServices
    {
        void Register<TService, TImplementation>() where TImplementation : class, TService where TService : class;

        void RegisterSingleton<TService, TImplementation>() where TImplementation : class, TService
            where TService : class;
    }
}