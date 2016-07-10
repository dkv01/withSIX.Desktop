// <copyright company="SIX Networks GmbH" file="RsyncDownloadProtocol.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Sync.Core.Transfer.Specs;

namespace SN.withSIX.Sync.Core.Transfer.Protocols
{
    
    public class RsyncException : DownloadException
    {
        public RsyncException(string message, string output = null, string parameters = null, Exception inner = null)
            : base(message, output, parameters, inner) {}
    }

    
    public class RsyncSoftException : RsyncException
    {
        public RsyncSoftException(string message, string output = null, string parameters = null, Exception inner = null)
            : base(message, output, parameters, inner) {}
    }

    public class RsyncDownloadProtocol : DownloadProtocol
    {
        static readonly IEnumerable<string> schemes = new[] {"rsync"};
        readonly IRsyncLauncher _rsyncLauncher;

        public RsyncDownloadProtocol(IRsyncLauncher rsyncLauncher) {
            _rsyncLauncher = rsyncLauncher;
        }

        public override IEnumerable<string> Schemes => schemes;

        public override async Task DownloadAsync(TransferSpec spec) {
            spec.Progress.Tries++;
            ConfirmSchemeSupported(spec.Uri.Scheme);
            ProcessExitResult(
                await
                    _rsyncLauncher.RunAndProcessAsync(spec.Progress, spec.Uri.ToString(), spec.LocalFile.ToString(),
                        spec.CancellationToken)
                        .ConfigureAwait(false), spec);
            VerifyIfNeeded(spec, spec.LocalFile);
        }

        public override void Download(TransferSpec spec) {
            spec.Progress.Tries++;
            ConfirmSchemeSupported(spec.Uri.Scheme);
            ProcessExitResult(
                _rsyncLauncher.RunAndProcess(spec.Progress, spec.Uri.ToString(), spec.LocalFile.ToString(),
                    spec.CancellationToken), spec);
            VerifyIfNeeded(spec, spec.LocalFile);
        }

        static void ProcessExitResult(ProcessExitResultWithOutput result, TransferSpec spec) {
            switch (result.ExitCode) {
            case 0:
                break;
            case -1:
                throw new RsyncSoftProgramException(
                    $"Aborted/Killed (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 5:
                throw new RsyncSoftException(
                    $"Server full (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 10:
                throw new RsyncSoftException(
                    $"Connection refused (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError,
                    result.StartInfo.Arguments);
            case 12:
                throw new RsyncSoftException(
                    $"Could not retrieve file (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 14:
                throw new RsyncSoftException(
                    $"Could not retrieve file due to IPC error (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 23:
                throw new RsyncSoftException(
                    $"Could not retrieve file (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 24:
                throw new RsyncSoftException(
                    $"Could not retrieve file (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 30:
                throw new RsyncSoftException(
                    $"Could not retrieve file due to Timeout (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 35:
                throw new RsyncSoftException(
                    $"Could not retrieve file due to Timeout (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
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

    public class RsyncCygwinProgramException : RsyncSoftProgramException, IProgramError, ICygwinProgramError
    {
        public RsyncCygwinProgramException(string message, string output = null, string parameters = null)
            : base(message, output, parameters) {}
    }

    public interface ICygwinProgramError {}

    public class RsyncSoftProgramException : RsyncSoftException, IProgramError
    {
        public RsyncSoftProgramException(string s, string s1, string arguments) : base(s, s1, arguments) {}
    }
}