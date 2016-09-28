// <copyright company="SIX Networks GmbH" file="ErrorHandlerr.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Applications.Errors;

namespace SN.withSIX.Core.Applications.Services
{
    public static class ErrorHandlerr
    {
        private static IExceptionHandler _exceptionHandler;

        public static Task GenerateDiagnosticZip(IAbsoluteFilePath path) {
            var d = GetLogFilesDictionary(Common.Paths.LocalDataRootPath);
            return
                Task.Run(
                    () => {
                        var items = d.Where(x => !x.Value.EndsWith("cef.log", StringComparison.CurrentCultureIgnoreCase));
                        Tools.CompressionUtil.PackFiles(items, path);
                    });
        }

        static Dictionary<string, string> GetLogFilesDictionary(IAbsoluteDirectoryPath path)
            => path.DirectoryInfo.GetFiles("*.log", SearchOption.AllDirectories)
                .ToDictionary(x => x.FullName.Replace(path + "\\", ""), x => x.FullName);

        public static Task<bool> TryAction(Func<Task> act, string action = null)
            => _exceptionHandler.TryExecuteAction(act, action);

        public static void SetExceptionHandler(IExceptionHandler handler) => _exceptionHandler = handler;

        public static UserErrorModel HandleException(Exception ex, string action = "Action")
            => _exceptionHandler.HandleException(ex, action);
    }
}