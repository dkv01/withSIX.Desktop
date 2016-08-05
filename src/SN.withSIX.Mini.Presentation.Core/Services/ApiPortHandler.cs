// <copyright company="SIX Networks GmbH" file="ApiPortHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Core.Services
{
    public class ApiPortHandler
    {
        private static string GetResourcePath(Assembly assembly, string path) {
            var resources = assembly.GetManifestResourceNames();
            var convertedPath = "." +
                                path.Replace("/", ".")
                                    .Replace("\\", ".");
            return resources.Single(x => x.EndsWith(convertedPath));
        }

        public static void SetupApiPort(IPEndPoint http, IPEndPoint https, IProcessManager pm) {
            if (https == null && http == null)
                throw new ArgumentException("Both value and valueHttp are unspecified");

            var tmpFolder = Common.Paths.TempPath.GetChildDirectoryWithName("apisetup");
            if (!tmpFolder.Exists)
                Directory.CreateDirectory(tmpFolder.ToString());
            try {
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var acct = sid.Translate(typeof (NTAccount)) as NTAccount;

                var commands = BuildCommands(http, https, tmpFolder, acct);
                if (Common.Flags.Verbose)
                    MainLog.Logger.Info("account name:" + acct);
                BuildAndRunBatFile(pm, tmpFolder, commands, true, true);
            } finally {
                if (tmpFolder.Exists)
                    tmpFolder.DirectoryInfo.Delete(true);
            }
        }

        private static IEnumerable<string> BuildCommands(IPEndPoint http, IPEndPoint https,
            IAbsoluteDirectoryPath tmpFolder, NTAccount acct) {
            var commands = new List<string> {
                "cd \"" + tmpFolder + "\""
            };

            if (http != null)
                commands.Add(BuildHttp(http, acct));

            if (https != null) {
                ExtractFile(tmpFolder, "server.pfx");
                commands.AddRange(BuildHttps(https, acct));
            }
            return commands;
        }

        private static void BuildAndRunBatFile(IProcessManager pm, IAbsoluteDirectoryPath tmpFolder,
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
                using (var p =
                    pm.Start(
                        new ProcessStartInfoBuilder(batFile) {
                            AsAdministrator = asAdministrator,
                            WorkingDirectory = tmpFolder,
                            WindowStyle = ProcessWindowStyle.Minimized
                        }.Build()))
                    p.WaitForExit();
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

        private static void ExtractFile(IAbsoluteDirectoryPath tmpFolder, string fileName) {
            var destinationFile = tmpFolder.GetChildFileWithName(fileName);
            var assembly = typeof (ApiPortHandler).Assembly;
            using (var s = assembly.GetManifestResourceStream(GetResourcePath(assembly, fileName)))
            using (
                var f = new FileStream(destinationFile.ToString(), FileMode.Create, FileAccess.ReadWrite, FileShare.None)
                )
                s.CopyTo(f);
        }

        private static string BuildHttp(IPEndPoint valueHttp, NTAccount acct)
            => "netsh http add urlacl url=http://" + valueHttp + "/ user=\"" + acct + "\"";

        private static string[] BuildHttps(IPEndPoint value, NTAccount acct) => new[] {
            "netsh http add urlacl url=https://" + value + "/ user=\"" + acct + "\"",
            "certutil -p localhost -importPFX server.pfx",
            "netsh http add sslcert ipport=" + value +
            " appid={12345678-db90-4b66-8b01-88f7af2e36bf} certhash=fca9282c0cd0394f61429bbbfdb59bacfc7338c9"
        };

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
                    if (tmpFolder.Exists) tmpFolder.Delete(true);
                }

                // 5. TODO: Restart firefox - however we do this already probably when opening the client?
            }

            private static IAbsoluteDirectoryPath[] GetProfiles() {
                var profileRoot =
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
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
                foreach (var p in Tools.Processes.FindProcess("firefox.exe"))
                    Tools.Processes.KillProcess(p);
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