// <copyright company="SIX Networks GmbH" file="IAssemblyService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;

namespace SN.withSIX.Core.Applications.Services
{
    public interface IAssemblyService
    {
        Assembly[] GetAllAssemblies();
        Type[] GetTypes(Assembly assembly);
    }
}