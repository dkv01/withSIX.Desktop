// <copyright company="SIX Networks GmbH" file="AssemblyService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using withSIX.Core.Applications.Services;

namespace withSIX.Core.Presentation.Bridge.Services
{
    public class AssemblyService : IAssemblyService, IPresentationService
    {
        public Assembly[] GetAllAssemblies() => AppDomain.CurrentDomain.GetAssemblies();

        public Type[] GetTypes(Assembly assembly) => assembly.GetTypes();
    }
}