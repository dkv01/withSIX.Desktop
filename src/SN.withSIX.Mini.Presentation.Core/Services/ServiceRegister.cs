using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleInjector;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Presentation.Core.Services
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
