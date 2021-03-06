﻿// <copyright company="SIX Networks GmbH" file="HttpDownloadProtocol.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Sync.Core.Legacy;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Sync.Core.Transfer.Protocols
{
    public interface IHttpDownloadProtocol : IDownloadProtocol {}

    public class HttpDownloadProtocol : DownloadProtocol, IHttpDownloadProtocol
    {
        static readonly TimeSpan timeout = TimeSpan.FromSeconds(60);
        static readonly IEnumerable<string> schemes = new[] {"http", "https", "ftp"};
        readonly Tools.FileTools.IFileOps _fileOps;
        private readonly Func<ExportLifetimeContext<IWebClient>> _webClientFactory;

        public HttpDownloadProtocol(Func<ExportLifetimeContext<IWebClient>> webClientFactory,
            Tools.FileTools.IFileOps fileOps) {
            _webClientFactory = webClientFactory;
            _fileOps = fileOps;
        }

        public override IEnumerable<string> Schemes => schemes;

        [Obsolete("Use async variant")]
        public override void Download(TransferSpec spec) {
            spec.Progress.Tries++;
            // Wrapping in Task.Run could solve deadlock issues
            Task.Run(() => DownloadAsync(spec)).WaitAndUnwrapException();
        }

        public override async Task DownloadAsync(TransferSpec spec) {
            spec.Progress.Tries++;
            ConfirmSchemeSupported(spec.Uri.Scheme);
            using (var webClient = _webClientFactory())
            using (webClient.Value.SetupDownloadTransferProgress(spec.Progress, timeout))
                await TryDownloadAsync(spec, webClient.Value, GetTmpFile(spec)).ConfigureAwait(false);
        }

        static IAbsoluteFilePath GetTmpFile(TransferSpec spec) => (spec.LocalFile + ".sixtmp").ToAbsoluteFilePath();

        static DownloadException CreateTimeoutException(TransferSpec spec, Exception e)
            => new DownloadSoftException("", null, null, new TimeoutException(
                $"The operation timedout in {timeout}. {CreateTransferExceptionMessage(spec)}", e));

        async Task TryDownloadAsync(TransferSpec spec, IWebClient webClient, IAbsoluteFilePath tmpFile) {
            try {
                tmpFile.RemoveReadonlyWhenExists();
                if (!string.IsNullOrWhiteSpace(spec.Uri.UserInfo))
                    webClient.SetAuthInfo(spec.Uri);
                using (webClient.HandleCancellationToken(spec))
                    await webClient.DownloadFileTaskAsync(spec.Uri, tmpFile.ToString()).ConfigureAwait(false);
                VerifyIfNeeded(spec, tmpFile);
                _fileOps.Move(tmpFile, spec.LocalFile);
            } catch (OperationCanceledException e) {
                _fileOps.DeleteIfExists(tmpFile.ToString());
                throw CreateTimeoutException(spec, e);
            } catch (WebException ex) {
                _fileOps.DeleteIfExists(tmpFile.ToString());
                var cancelledEx = ex.InnerException as OperationCanceledException;
                if (cancelledEx != null)
                    throw CreateTimeoutException(spec, cancelledEx);
                if (ex.Status == WebExceptionStatus.RequestCanceled)
                    throw CreateTimeoutException(spec, ex);

                var response = ex.Response as HttpWebResponse;
                if (response == null)
                    throw GenerateDownloadException(spec, ex);

                switch (response.StatusCode) {
                case HttpStatusCode.NotFound:
                    throw new RequestFailedException("Received a 404: NotFound response", ex);
                case HttpStatusCode.Forbidden:
                    throw new RequestFailedException("Received a 403: Forbidden response", ex);
                case HttpStatusCode.Unauthorized:
                    throw new RequestFailedException("Received a 401: Unauthorized response", ex);
                }
                throw GenerateDownloadException(spec, ex);
            }
        }

        static Exception GenerateDownloadException(TransferSpec spec, WebException e) {
            // TODO: Or should we rather abstract this away into the downloader exceptions instead?
            var r = e.Response as HttpWebResponse;
            if (r != null) {
                return new HttpDownloadException(e.Message + ". " + CreateTransferExceptionMessage(spec), e) {
                    StatusCode = r.StatusCode
                };
            }
            /*
            var r2 = e.Response as FtpWebResponse;
            if (r2 != null) {
                return new FtpDownloadException(e.Message + ". " + CreateTransferExceptionMessage(spec), e) {
                    StatusCode = r2.StatusCode
                };
            }
            */
            return new DownloadException(e.Message + ". " + CreateTransferExceptionMessage(spec), e);
        }
    }
}