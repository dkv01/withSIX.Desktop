// <copyright company="SIX Networks GmbH" file="RsyncUploadProtocol.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Sync.Core.Transfer.Specs;

namespace SN.withSIX.Sync.Core.Transfer.Protocols
{
    public class RsyncUploadProtocol : UploadProtocol
    {
        static readonly IEnumerable<string> schemes = new[] {"rsync"};
        readonly IRsyncLauncher _rsyncLauncher;

        public RsyncUploadProtocol(IRsyncLauncher rsyncLauncher) {
            _rsyncLauncher = rsyncLauncher;
        }

        public override IEnumerable<string> Schemes => schemes;

        public override async Task UploadAsync(TransferSpec spec) {
            spec.Progress.Tries++;
            ConfirmSchemeSupported(spec.Uri.Scheme);
            ProcessExitResult(
                await
                    _rsyncLauncher.RunAndProcessAsync(spec.Progress, spec.LocalFile.ToString(), spec.Uri.ToString())
                        .ConfigureAwait(false), spec);
        }

        public override void Upload(TransferSpec spec) {
            spec.Progress.Tries++;
            ConfirmSchemeSupported(spec.Uri.Scheme);
            ProcessExitResult(
                _rsyncLauncher.RunAndProcess(spec.Progress, spec.LocalFile.ToString(), spec.Uri.ToString()), spec);
        }

        static void ProcessExitResult(ProcessExitResultWithOutput result, TransferSpec spec) {
            switch (result.ExitCode) {
            case 0:
                break;
            case -1:
                throw new RsyncSoftProgramException(
                    $"Aborted/Killed (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 35584:
                throw new RsyncSoftProgramException(
                    $"Stackdump (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case -1073741819:
                throw new RsyncCygwinProgramException(
                    $"Cygwin fork/rebase problem (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            default:
                throw new RsyncException(
                    $"Did not exit gracefully (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            }
        }
    }
}