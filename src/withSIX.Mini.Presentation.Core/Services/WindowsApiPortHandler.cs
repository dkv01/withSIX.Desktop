using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Core.Services.Infrastructure;

namespace withSIX.Mini.Presentation.Core.Services
{
    public class WindowsApiPortHandler : WindowsApiPortHandlerBase
    {
        public static async Task SetupApiPort(IPEndPoint http, IPEndPoint https, IProcessManager pm) {
            if ((https == null) && (http == null))
                throw new ArgumentException("Both value and valueHttp are unspecified");

            //var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            //var acct = sid.Translate(typeof(NTAccount)) as NTAccount;

            var tmpFolder = Common.Paths.TempPath.GetChildDirectoryWithName("apisetup");
            var commands = BuildCommands(http, https, tmpFolder).ToList();
            if (!commands.Any())
                return;
            if (!tmpFolder.Exists)
                Directory.CreateDirectory(tmpFolder.ToString());
            try {
                await BuildAndRunBatFile(pm, tmpFolder, commands, true, true).ConfigureAwait(false);
            } finally {
                if (tmpFolder.Exists)
                    tmpFolder.DirectoryInfo.Delete(true);
            }
        }

        private static IEnumerable<string> BuildCommands(IPEndPoint http, IPEndPoint https,
            IAbsoluteDirectoryPath tmpFolder) {
            var commands = new List<string> {
                "cd \"" + tmpFolder + "\""
            };

            //if (http != null)
            //  commands.Add(BuildHttp(http));

            if (https != null) {
                ExtractFile(tmpFolder, "server.pfx");
                commands.AddRange(BuildHttps(https));
            }
            return commands;
        }

        private static string BuildHttp(IPEndPoint valueHttp, NTAccount acct)
            => "netsh http add urlacl url=http://" + valueHttp + "/ user=\"" + acct + "\"";

        private static string[] BuildHttps(IPEndPoint value) => new[] {
            //"netsh http add urlacl url=https://" + value + "/ user=\"" + acct + "\"",
            "certutil -p localhost -importPFX server.pfx",
            //"netsh http add sslcert ipport=" + value + " appid={12345678-db90-4b66-8b01-88f7af2e36bf} certhash=fca9282c0cd0394f61429bbbfdb59bacfc7338c9"
        };
    }
}