// <copyright company="SIX Networks GmbH" file="CommonBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Services;

namespace withSIX.Core
{
    public static class CommonBase
    {
        public static IAssemblyLoader AssemblyLoader { get; set; } // = new AssemblyLoader(Assembly.GetEntryAssembly());
    }
}