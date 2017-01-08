// <copyright company="SIX Networks GmbH" file="StartWithWindowsHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Applications.NotificationHandlers
{
    public class StartWithWindowsHandler
    {
        public void HandleStartWithWindows(bool startWithWindows) {
            if (!Common.IsWindows)
                throw new PlatformNotSupportedException();
            var rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (startWithWindows) {
                var cmd = GenerateCommandLineExecution(Common.Paths.EntryLocation, "Sync.exe", "--hide");
                // Add the value in the registry so that the application runs at startup
                rkApp.SetValue("withSIX", cmd.CombineParameters());
            } else {
                // Remove the value from the registry so that the application doesn't start
                rkApp.DeleteValue("withSIX", false);
            }
        }

        public static IEnumerable<string> GenerateCommandLineExecution(IAbsoluteFilePath location, string executable,
            params string[] desiredParams) {
            var updateExe = GetUpdateExe(location);
            return updateExe != null && updateExe.Exists
                ? new[] {updateExe.ToString()}.Concat(Restarter.BuildUpdateExeArguments(executable, desiredParams))
                : new[] {location.ToString()}.Concat(desiredParams);
        }

        public static IAbsoluteFilePath GetUpdateExe(IAbsoluteFilePath location) {
            var parent = location.ParentDirectoryPath;
            var updateExe = parent.HasParentDirectory
                ? parent.ParentDirectoryPath.GetChildFileWithName("Update.exe")
                : null;
            return updateExe;
        }
    }
}