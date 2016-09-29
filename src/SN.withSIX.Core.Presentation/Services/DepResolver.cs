// <copyright company="SIX Networks GmbH" file="DepResolver.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using SimpleInjector;
using withSIX.Core.Applications.Factories;

namespace withSIX.Core.Presentation.Services
{
    public class DepResolver : IDepResolver, IPresentationService
    {
        readonly Container _container;

        public DepResolver(Container container) {
            _container = container;
        }

        public T GetInstance<T>() where T : class => _container.GetInstance<T>();
        public object GetInstance(Type t) => _container.GetInstance(t);

        public IEnumerable<T> GetInstances<T>() where T : class => _container.GetAllInstances<T>();
    }
}