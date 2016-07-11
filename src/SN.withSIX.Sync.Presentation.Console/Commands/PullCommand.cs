// <copyright company="SIX Networks GmbH" file="PullCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>


using SN.withSIX.Core;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Sync.Presentation.Console.Commands
{

    public class PullCommand : BaseCommand
    {
        readonly IRsyncLauncher _rsyncLauncher;
        public string Key;

        public PullCommand(IRsyncLauncher rsyncLauncher) {
            _rsyncLauncher = rsyncLauncher;
            HasAdditionalArguments(2, "<source> <destination>");
            HasOption("k|key=", "Path to key", s => Key = s);
            IsCommand("pull", "Rsync Pull");
        }

        public override int Run(params string[] remainingArguments) {
            var src = remainingArguments[0];
            var dst = remainingArguments[1];

            System.Console.WriteLine("Pulling...");

            var rm = new RsyncController(dst, src, Key, _rsyncLauncher);
            var status = new TransferStatus(src);
            using (
                new TimerWithoutOverlap(200,
                    () =>
                        System.Console.Write("\r" + status.Progress + "% " + (status.Speed.HasValue ? Tools.FileUtil.GetFileSize(status.Speed.Value) : "0 b") +
                                             "/s                    ")))
                rm.Pull(status);
            System.Console.Write("\n100% " + (status.Speed.HasValue ? Tools.FileUtil.GetFileSize(status.Speed.Value) : "0 b") + "/s                    ");
            System.Console.WriteLine("\nCompleted");

            return 0;
        }
    }
}