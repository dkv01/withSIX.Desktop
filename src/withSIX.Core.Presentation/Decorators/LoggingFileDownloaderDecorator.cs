// <copyright company="SIX Networks GmbH" file="LoggingFileDownloaderDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Core.Presentation.Decorators
{
    public class LoggingFileDownloaderDecorator : LoggingFileTransferDecorator, IFileDownloader
    {
        readonly IFileDownloader _downloader;

        public LoggingFileDownloaderDecorator(IFileDownloader downloader) {
            _downloader = downloader;
        }

        public void Download(FileDownloadSpec spec) {
            Wrap(() => _downloader.Download(spec), spec);
        }

        public Task DownloadAsync(FileDownloadSpec spec) => Wrap(() => _downloader.DownloadAsync(spec), spec);

        protected override void OnStart(TransferSpec spec)
            => this.Logger().Info("Started download of {0} to {1}", spec.Uri, spec.LocalFile);

        protected override void OnFinished(TransferSpec spec)
            => this.Logger().Info("Finished download of {0} to {1}", spec.Uri, spec.LocalFile);

        protected override void OnError(TransferSpec spec, Exception e) {
            if (e is OperationCanceledException) {
                this.Logger()
                    .Warn(
                        $"Cancelled download of {spec.Uri} to {spec.LocalFile}, try {spec.Progress.Tries} ({e.Message})");
                return;
            }
            var msg =
                $"Failed download of {spec.Uri} to {spec.LocalFile}, try {spec.Progress.Tries} ({e.Message})\nOutput: {spec.Progress.Output}\n\nError report: {e.Format(1)}";
            if (spec.Progress.Tries > 1)
                this.Logger().Warn(msg);
            else
                this.Logger().Error(msg);
        }
    }
}