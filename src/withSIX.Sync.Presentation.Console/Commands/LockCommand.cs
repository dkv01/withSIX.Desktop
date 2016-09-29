// <copyright company="SIX Networks GmbH" file="LockCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using NDepend.Path;

using SN.withSIX.Core;

namespace SN.withSIX.Sync.Presentation.Console.Commands
{

    public class LockCommand : BaseCommand
    {
        public string RepoDir;

        public LockCommand() {
            IsCommand("lock", "Lock specified repo until command finishes");
            HasOption("r|repodir=", SynqStrings.RepoDirStr, r => RepoDir = r);
            HasAdditionalArguments(1, "Command to execute");
        }

        public override int Run(string[] remainingArguments) {
            using (GetRepo(RepoDir.ToAbsoluteDirectoryPath()))
                StartBat(remainingArguments.First().ToAbsoluteFilePath());
            return 0;
        }

        static void StartBat(IAbsoluteFilePath bat) {
            using (var p = new Process {StartInfo = GetCmdExeStartupParams(bat)}) {
                p.Start();
                p.WaitForExit();
            }
        }

        static ProcessStartInfo GetCmdExeStartupParams(IAbsoluteFilePath bat, string arguments = null) => new ProcessStartInfo(Common.Paths.CmdExe.ToString(),
            $"/C \"\"{bat}\" {arguments}\"") {
            WorkingDirectory = bat.ParentDirectoryPath.ToString()
        };
    }
}