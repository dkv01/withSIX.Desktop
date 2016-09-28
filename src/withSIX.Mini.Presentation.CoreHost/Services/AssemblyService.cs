// <copyright company="SIX Networks GmbH" file="AssemblyService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation;

namespace withSIX.Mini.Presentation.CoreHost.Services
{
    public class AssemblyService : IAssemblyService, IPresentationService
    {
        public static Assembly[] AllAssemblies { get; set; }
        public Assembly[] GetAllAssemblies() => AllAssemblies;

        public Type[] GetTypes(Assembly assembly) => assembly.GetTypes();
    }
}