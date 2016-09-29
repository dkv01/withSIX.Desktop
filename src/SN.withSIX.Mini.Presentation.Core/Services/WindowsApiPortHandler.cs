using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;

namespace SN.withSIX.Mini.Presentation.Core.Services
{
    public class WindowsApiPortHandler : WindowsApiPortHandlerBase
    {
        public static void SetupApiPort(IPEndPoint http, IPEndPoint https, IProcessManager pm) {
            if ((https == null) && (http == null))
                throw new ArgumentException("Both value and valueHttp are unspecified");

            var tmpFolder = Common.Paths.TempPath.GetChildDirectoryWithName("apisetup");
            if (!tmpFolder.Exists)
                Directory.CreateDirectory(tmpFolder.ToString());
            try {
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var acct = sid.Translate(typeof(NTAccount)) as NTAccount;

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


        private static string BuildHttp(IPEndPoint valueHttp, NTAccount acct)
            => "netsh http add urlacl url=http://" + valueHttp + "/ user=\"" + acct + "\"";

        private static string[] BuildHttps(IPEndPoint value, NTAccount acct) => new[] {
            "netsh http add urlacl url=https://" + value + "/ user=\"" + acct + "\"",
            "certutil -p localhost -importPFX server.pfx",
            "netsh http add sslcert ipport=" + value +
            " appid={12345678-db90-4b66-8b01-88f7af2e36bf} certhash=fca9282c0cd0394f61429bbbfdb59bacfc7338c9"
        };
    }
}