// <copyright company="SIX Networks GmbH" file="LoggingFileUploaderDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Core.Logging;
using SN.withSIX.Sync.Core.Transfer;
using SN.withSIX.Sync.Core.Transfer.Specs;

namespace SN.withSIX.Core.Presentation.Decorators
{
    /// <summary>
    ///     Adds logging capability to the file uploader
    /// </summary>
    public class LoggingFileUploaderDecorator : LoggingFileTransferDecorator, IFileUploader
    {
        readonly IFileUploader _uploader;

        public LoggingFileUploaderDecorator(IFileUploader uploader) {
            _uploader = uploader;
        }

        public void Upload(FileUploadSpec spec) {
            Wrap(() => _uploader.Upload(spec), spec);
        }

        public Task UploadAsync(FileUploadSpec spec) => Wrap(() => _uploader.UploadAsync(spec), spec);

        protected override void OnStart(TransferSpec spec) {
            this.Logger().Info("Started upload of {0} to {1}", spec.LocalFile, spec.Uri);
        }

        protected override void OnFinished(TransferSpec spec) {
            this.Logger().Info("Finished upload of {0} to {1}", spec.LocalFile, spec.Uri);
        }

        protected override void OnError(TransferSpec spec, Exception e) {
            var msg = $"Failed upload of {spec.LocalFile} to {spec.Uri} ({e.Message})";
            if (spec.Progress.Tries <= 1)
                this.Logger().Error(msg);
            else
                this.Logger().Warn(msg);
        }
    }
}