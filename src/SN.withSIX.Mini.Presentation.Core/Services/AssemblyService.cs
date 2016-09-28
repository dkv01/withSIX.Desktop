// <copyright company="SIX Networks GmbH" file="AssemblyService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Infra.Data.Services;

namespace SN.withSIX.Mini.Presentation.Core.Services
{
    public class AssemblyService : IAssemblyService, IPresentationService
    {
        public Assembly[] GetAllAssemblies() => AppDomain.CurrentDomain.GetAssemblies();

        public Type[] GetTypes(Assembly assembly) => assembly.GetTypes();
    }
}