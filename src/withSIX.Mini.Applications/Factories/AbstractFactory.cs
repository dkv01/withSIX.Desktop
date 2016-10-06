// <copyright company="SIX Networks GmbH" file="AbstractFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using withSIX.Core.Applications.Factories;

namespace withSIX.Mini.Applications.Factories
{
    class AbstractFactory
    {
        readonly IDepResolver _depResolver;

        public AbstractFactory(IDepResolver depResolver) {
            Contract.Requires<ArgumentNullException>(depResolver != null);
            _depResolver = depResolver;
        }
    
        protected T GetInstance<T>() where T : class => _depResolver.GetInstance<T>();
    }
}