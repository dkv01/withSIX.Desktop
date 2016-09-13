// <copyright company="SIX Networks GmbH" file="ZsyncDownloadProtocol.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NDepend.Path;

using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Sync.Core.Transfer.Protocols.Handlers;
using SN.withSIX.Sync.Core.Transfer.Specs;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Sync.Core.Transfer.Protocols
{
    
    public class ZsyncException : DownloadException
    {
        public ZsyncException(string message, string output = null, string parameters = null, Exception inner = null)
            : base(message, output, parameters, inner) {}
    }

    
    public class ZsyncSoftException : ZsyncException
    {
        public ZsyncSoftException(string message, string output = null, string parameters = null, Exception inner = null)
            : base(message, output, parameters, inner) {}
    }

    
    public class ZsyncLoopDetectedException : ZsyncSoftException
    {
        public ZsyncLoopDetectedException(string message, string output = null, string parameters = null,
            Exception inner = null)
            : base(message, output, parameters, inner) {}
    }

    
    public class ZsyncIncompatibleException : ZsyncSoftException
    {
        public ZsyncIncompatibleException(string message, string output = null, string parameters = null,
            Exception inner = null)
            : base(message, output, parameters, inner) {}
    }

    
    public class ZsyncDownloadProtocol : DownloadProtocol
    {
        static readonly string[] schemes = {"zsync", "zsyncs"};
        readonly IZsyncLauncher _zsyncLauncher;

        public ZsyncDownloadProtocol(IZsyncLauncher zsyncLauncher) {
            _zsyncLauncher = zsyncLauncher;
        }

        public override IEnumerable<string> Schemes => schemes;

        public override async Task DownloadAsync(TransferSpec spec) {
            spec.Progress.Tries++;
            ConfirmSchemeSupported(spec.Uri.Scheme);
            spec.Progress.ResetZsyncLoopInfo();
            ProcessExitResult(await _zsyncLauncher.RunAndProcessAsync(GetParams(spec)).ConfigureAwait(false),
                spec);
            VerifyIfNeeded(spec, spec.LocalFile);
        }

        private static ZsyncParams GetParams(TransferSpec spec) => new ZsyncParams(spec.Progress,
            new Uri(FixUrl(spec.Uri)), spec.LocalFile) {
                CancelToken = spec.CancellationToken,
                ExistingFile = spec.ExistingFile
            };

        public override void Download(TransferSpec spec) {
            spec.Progress.Tries++;
            ConfirmSchemeSupported(spec.Uri.Scheme);
            spec.Progress.ResetZsyncLoopInfo();
            ProcessExitResult(_zsyncLauncher.RunAndProcess(GetParams(spec)), spec);
            VerifyIfNeeded(spec, spec.LocalFile);
        }

        protected override void VerifyIfNeeded(TransferSpec spec, IAbsoluteFilePath localFile) {
            // To make sure we dont fall into zsync silent failure trap..
            if (!localFile.Exists)
                throw new ZsyncSoftException("Download failure, file doesn't exist");
            base.VerifyIfNeeded(spec, localFile);
        }

        static void ProcessExitResult(ProcessExitResultWithOutput result, TransferSpec spec) {
            if (spec.Progress.ZsyncIncompatible) {
                throw new ZsyncIncompatibleException(
                    $"Zsync Incompatible (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError,
                    result.StartInfo.Arguments);
            }

            switch (result.ExitCode) {
            case 0:
                break;
            case -1: {
                if (spec.Progress.ZsyncLoopCount >= 2) {
                    throw new ZsyncLoopDetectedException(
                        $"Loop detected, aborted transfer (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                        result.StandardOutput + result.StandardError,
                        result.StartInfo.Arguments);
                }

                throw new ZsyncSoftProgramException(
                    $"Aborted/Killed (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError,
                    result.StartInfo.Arguments);
            }
            case 1:
                throw new ZsyncSoftException(
                    $"Could not retrieve file due to protocol error (not a zsync file?) (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 2:
                throw new ZsyncSoftException(
                    $"Connection reset (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 3:
                var statusCode = FindStatusCode("" + result.StandardOutput + result.StandardError);
                try {
                    throw new ZsyncSoftException(
                        $"Could not retrieve file (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                        result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
                } catch (ZsyncSoftException ex) {
                    switch (statusCode) {
                    case -1:
                        throw;
                    case 404:
                        throw new RequestFailedException("Received a 404: NotFound response", ex);
                    case 403:
                        throw new RequestFailedException("Received a 403: Forbidden response", ex);
                    case 401:
                        throw new RequestFailedException("Received a 401: Unauthorized response", ex);
                    }
                    throw;
                }
            case 21:
                throw new ZsyncSoftProgramException(
                    $"Retrieved file but could not rename (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case 35584:
                throw new ZsyncSoftProgramException(
                    $"Stackdump (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            case -1073741819:
                throw new ZsyncCygwinProgramException(
                    $"Cygwin fork/rebase problem (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            default:
                throw new ZsyncException(
                    $"Did not exit gracefully (PID: {result.Id}, Status: {result.ExitCode}). {CreateTransferExceptionMessage(spec)}",
                    result.StandardOutput + result.StandardError, result.StartInfo.Arguments);
            }
        }

        static string FixUrl(Uri uri) => uri.Scheme == "zsync" || uri.Scheme == "zsyncs"
            ? uri.ReplaceZsyncProtocol() + ".zsync"
            : uri + ".zsync";

        private static readonly Regex rx = new Regex(@"Got HTTP (\d+) (expected \d+)");
        static int FindStatusCode(string output) {
            if (output == null)
                return -1;
            var m = rx.Match(output);
            if (m.Success)
                return Convert.ToInt32(m.Groups[1].Value);
            return -1;
        }
    }

    public class ZsyncCygwinProgramException : ZsyncSoftProgramException, IProgramError, ICygwinProgramError
    {
        public ZsyncCygwinProgramException(string message, string output = null, string parameters = null) : base(message, output, parameters) { }
    }

    public class ZsyncSoftProgramException : ZsyncSoftException, IProgramError
    {
        public ZsyncSoftProgramException(string s, string s1, string arguments) : base(s, s1, arguments) {}
    }

    public interface IProgramError {}


    public class ZsyncDownloadWithHttpFallbackProtocol : ZsyncDownloadProtocol
    {
        readonly IHttpDownloadProtocol _httpDownloader;

        public ZsyncDownloadWithHttpFallbackProtocol(IZsyncLauncher zsyncLauncher,
            IHttpDownloadProtocol httpDownloader) : base(zsyncLauncher) {
            _httpDownloader = httpDownloader;
        }

        public override void Download(TransferSpec spec) {
            TryDownload(spec);
        }

        public override Task DownloadAsync(TransferSpec spec) => TryDownloadAsync(spec);

        async Task TryDownloadAsync(TransferSpec spec) {
            Exception retryEx = null;
            try {
                await base.DownloadAsync(spec).ConfigureAwait(false);
            } catch (ZsyncIncompatibleException e) {
                retryEx = e;
            } catch (ZsyncLoopDetectedException e) {
                retryEx = e;
            } catch (ZsyncCygwinProgramException e) {
                // Rebase issue..
                retryEx = e;
            } catch (ZsyncSoftException e) {
                var progress = spec.Progress;
                if (progress != null && !AllowZsyncFallback(progress))
                    throw;
                retryEx = e;
            }
            if (retryEx != null)
                await TryRegularHttpDownloadAsync(spec, retryEx).ConfigureAwait(false);
        }

        static bool AllowZsyncFallback(ITransferProgress progress)
            => progress.ZsyncHttpFallback || progress.Tries > progress.ZsyncHttpFallbackAfter;

        void TryDownload(TransferSpec spec) {
            try {
                base.Download(spec);
            } catch (ZsyncIncompatibleException e) {
                TryRegularHttpDownload(spec, e);
            } catch (ZsyncLoopDetectedException e) {
                TryRegularHttpDownload(spec, e);
            } catch (ZsyncSoftException e) {
                var progress = spec.Progress;
                if (progress != null && !AllowZsyncFallback(progress))
                    throw;
                TryRegularHttpDownload(spec, e);
            }
        }

        protected virtual void TryRegularHttpDownload(TransferSpec spec, Exception exception) {
            spec.Progress.Tries++;
            _httpDownloader.Download(new FileDownloadSpec(spec.Uri.ReplaceZsyncProtocol(), spec.LocalFile, spec.Progress) {
                CancellationToken = spec.CancellationToken
            });
        }

        protected virtual Task TryRegularHttpDownloadAsync(TransferSpec spec, Exception exception) {
            spec.Progress.Tries++;
            return
                _httpDownloader.DownloadAsync(new FileDownloadSpec(spec.Uri.ReplaceZsyncProtocol(), spec.LocalFile,
                    spec.Progress) {CancellationToken = spec.CancellationToken});
        }
    }

    static class UriExtensions
    {
        internal static string ReplaceZsyncProtocol(this Uri uri)
            => uri.ToString().Replace("zsync://", "http://").Replace("zsyncs://", "https://");
    }

    public class LoggingZsyncDownloadWithHttpFallbackProtocol : ZsyncDownloadWithHttpFallbackProtocol, IEnableLogging
    {
        public LoggingZsyncDownloadWithHttpFallbackProtocol(IZsyncLauncher zsyncLauncher,
            IHttpDownloadProtocol httpDownloader) : base(zsyncLauncher, httpDownloader) {}

        protected override void TryRegularHttpDownload(TransferSpec spec, Exception e) {
            LogMessage(spec, e);
            base.TryRegularHttpDownload(spec, e);
        }

        protected override Task TryRegularHttpDownloadAsync(TransferSpec spec, Exception e) {
            LogMessage(spec, e);
            return base.TryRegularHttpDownloadAsync(spec, e);
        }

        void LogMessage(TransferSpec spec, Exception e) {
            var tfex = e as DownloadException;
            string o = null;
            if (tfex != null)
                o = "\nOutput: " + tfex.Output;
            this.Logger()
                .Warn("Performing http fallback for {0} due to {1} ({2}){3}", spec.Uri.AuthlessUri(), e.GetType(),
                    e.Message, o);
        }
    }
}