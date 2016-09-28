// <copyright company="SIX Networks GmbH" file="ApiPortHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Core.Services
{
    public class ApiPortHandlerBase
    {
        protected static void BuildAndRunBatFile(IProcessManager pm, IAbsoluteDirectoryPath tmpFolder,
            IEnumerable<string> commands, bool asAdministrator = false, bool noisy = false) {
            var batFile = tmpFolder.GetChildFileWithName("install.bat");
            var actualCommands =
                new[] {"chcp 65001"}.Concat(commands)
                    .Concat(new[] {"echo finished"})
                    .Select(x => x == "" ? x : x + " >> install.log");
            var commandBat = string.Join("\r\n",
                new[] {"", "echo starting > install.log"}.Concat(actualCommands)
                    .Concat(new[] {""}));
            var encoding = Encoding.UTF8;
            File.WriteAllText(batFile.ToString(), commandBat, encoding);
            if (Common.Flags.Verbose || noisy)
                MainLog.Logger.Info("install.bat content:\n" + commandBat);

            try {
                var pInfo = new ProcessStartInfoBuilder(batFile) {
                    WorkingDirectory = tmpFolder
                    //WindowStyle = ProcessWindowStyle.Minimized
                }.Build();
                pInfo.CreateNoWindow = true;
                var basicLaunchInfo = new BasicLaunchInfo(pInfo) {StartMinimized = true};
                var r = asAdministrator ? pm.LaunchElevated(basicLaunchInfo) : pm.Launch(basicLaunchInfo);
                r.ConfirmSuccess();
            } catch (Win32Exception ex) {
                if (ex.IsElevationCancelled())
                    throw ex.HandleUserCancelled();
                throw;
            }
            var logFile = tmpFolder.GetChildFileWithName("install.log");
            var output = File.ReadAllText(logFile.ToString(), encoding);

            if (Common.Flags.Verbose || noisy)
                MainLog.Logger.Info("install.bat output:\n" + output);
        }

        protected static void ExtractFile(IAbsoluteDirectoryPath tmpFolder, string fileName) {
            var destinationFile = tmpFolder.GetChildFileWithName(fileName);
            var assembly = typeof(ApiPortHandlerBase).GetTypeInfo().Assembly;
            using (var s = assembly.GetManifestResourceStream(GetResourcePath(assembly, fileName)))
            using (
                var f = new FileStream(destinationFile.ToString(), FileMode.Create, FileAccess.ReadWrite, FileShare.None)
            )
                s.CopyTo(f);
        }

        protected static string GetResourcePath(Assembly assembly, string path) {
            var resources = assembly.GetManifestResourceNames();
            var convertedPath = "." +
                                path.Replace("/", ".")
                                    .Replace("\\", ".");
            return resources.Single(x => x.EndsWith(convertedPath));
        }
    }

    public class FirefoxHandler : ApiPortHandlerBase
    {
        public static void SetupFirefox(IProcessManager pm) => new FireFoxCertInstaller().Install(pm);

        private class FireFoxCertInstaller
        {
            internal void Install(IProcessManager pm) {
                const string certFileName = "server.cer";

                // 1. Find the FF Profiles, if found, proceed
                var profiles = GetProfiles();
                if (!profiles.Any())
                    return;

                // 2. Unpack cert and tools
                var tmpFolder = Common.Paths.TempPath.GetChildDirectoryWithName("firefox");
                if (!tmpFolder.Exists)
                    Directory.CreateDirectory(tmpFolder.ToString());
                try {
                    var toolLocation = tmpFolder.GetChildDirectoryWithName(@"nss-3.11");
                    Tools.Compression.Unpack(Common.Paths.AppPath.GetChildFileWithName("nss-3.11.zip"), toolLocation);

                    var todoProfiles = GetTodos(pm, profiles, toolLocation, tmpFolder);
                    if (!todoProfiles.Any())
                        return;

                    // 3. Close running FF instances (IF WE DID NOT INSTALL A CERT BEFOREHAND)
                    TerminateFirefox();

                    // 4. add lib and bin to path, Install cert
                    InstallCerts(pm, toolLocation, tmpFolder, todoProfiles, certFileName);
                } finally {
                    if (tmpFolder.Exists)
                        tmpFolder.Delete(true);
                }

                // 5. TODO: Restart firefox - however we do this already probably when opening the client?
            }

            private static IAbsoluteDirectoryPath[] GetProfiles() {
                var profileRoot =
                    PathConfiguration.GetFolderPath(EnvironmentSpecial.SpecialFolder.ApplicationData)
                        .ToAbsoluteDirectoryPath()
                        .GetChildDirectoryWithName(@"Mozilla\Firefox\Profiles");

                if (!profileRoot.Exists)
                    return new IAbsoluteDirectoryPath[0];

                return profileRoot.DirectoryInfo.EnumerateDirectories()
                    .Select(x => x.FullName.ToAbsoluteDirectoryPath())
                    .Where(x => x.GetChildFileWithName("cert8.db").Exists).ToArray();
            }

            private IAbsoluteDirectoryPath[] GetTodos(IProcessManager pm, IAbsoluteDirectoryPath[] profiles,
                    IAbsoluteDirectoryPath toolLocation, IAbsoluteDirectoryPath tmpFolder)
                => profiles.Where(x => ShouldInstall(pm, toolLocation, tmpFolder, x)).ToArray();

            private static void TerminateFirefox() {
                foreach (var p in Tools.ProcessManager.Management.FindProcess("firefox.exe"))
                    Tools.ProcessManager.Management.KillProcess(p);
            }

            private static void InstallCerts(IProcessManager pm, IAbsoluteDirectoryPath toolLocation,
                IAbsoluteDirectoryPath tmpFolder, IEnumerable<IAbsoluteDirectoryPath> todoProfiles,
                string certFileName) {
                ExtractFile(tmpFolder, certFileName);
                var certFile = tmpFolder.GetChildFileWithName(certFileName);
                RunCertCommands(pm, toolLocation, tmpFolder,
                    todoProfiles.Select(p => BuildInstallCommand(toolLocation, certFile, p)).ToArray());
            }

            private bool ShouldInstall(IProcessManager pm, IAbsoluteDirectoryPath toolLocation,
                IAbsoluteDirectoryPath tmpFolder, IAbsoluteDirectoryPath p) {
                RunCertCommands(pm, toolLocation, tmpFolder, BuildCheckCommand(toolLocation, p));
                return
                    !File.ReadAllText(tmpFolder.GetChildFileWithName("install.log").ToString())
                        .Contains("FC:A9:28:2C:0C:D0:39:4F:61:42:9B:BB:FD:B5:9B:AC:FC:73:38:C9");
            }

            private static void RunCertCommands(IProcessManager pm, IAbsoluteDirectoryPath toolLocation,
                IAbsoluteDirectoryPath tmpFolder, params string[] certCommands) {
                var commands =
                    new List<string> {
                        $@"set PATH={toolLocation}\bin;{toolLocation}\lib;%PATH%"
                    };
                commands.AddRange(certCommands);
                BuildAndRunBatFile(pm, tmpFolder, commands);
            }

            private static string BuildCheckCommand(IAbsoluteDirectoryPath toolLocation, IAbsoluteDirectoryPath p)
                =>
                $@"""{toolLocation}\bin\certutil"" -L -n ""withSIX Sync local"" -t ""CT,C,C"" -d ""{p}""" +
                " > install.log";

            private static string BuildInstallCommand(IAbsoluteDirectoryPath toolLocation, IAbsoluteFilePath certFile,
                    IAbsoluteDirectoryPath p)
                =>
                $@"""{toolLocation}\bin\certutil"" -A -n ""withSIX Sync local"" -t ""CT,C,C"" -i ""{certFile}"" -d ""{p}""";
        }
    }
}