// <copyright company="SIX Networks GmbH" file="IDepResolver.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;

namespace withSIX.Core.Applications.Factories
{
    public interface IDepResolver
    {
        T GetInstance<T>() where T : class;
        object GetInstance(Type t);
        IEnumerable<T> GetInstances<T>() where T : class;
    }
}