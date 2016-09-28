// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Reflection;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Services;

namespace withSIX.Mini.Presentation.CoreHost
{
    public class Program
    {
        public static void Main(string[] args) {
            var entryAssembly = typeof(Program).GetTypeInfo().Assembly;
            var rootPath = entryAssembly.Location.ToAbsoluteFilePath().ParentDirectoryPath;
            CommonBase.AssemblyLoader = new AssemblyLoader(entryAssembly, null, rootPath);

            var bs = new CoreAppBootstrapper(args, rootPath);
            bs.Configure();
        }
    }
}