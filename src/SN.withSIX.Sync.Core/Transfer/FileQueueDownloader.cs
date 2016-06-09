﻿// <copyright company="SIX Networks GmbH" file="FileQueueDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Sync.Core.Legacy;
using SN.withSIX.Sync.Core.Transfer.Specs;

namespace SN.withSIX.Sync.Core.Transfer
{
    // TODO: cleanup token handling...
    public class FileQueueDownloader : IFileQueueDownloader
    {
        protected readonly IMultiMirrorFileDownloader Downloader;

        public FileQueueDownloader(IMultiMirrorFileDownloader downloader) {
            Downloader = downloader;
        }

        public virtual async Task DownloadAsync(FileQueueSpec spec) {
            foreach (var file in spec.Files) {
                await
                    Downloader.DownloadAsync(GetDlSpec(spec, file)).ConfigureAwait(false);
            }
        }

        public virtual async Task DownloadAsync(FileQueueSpec spec, CancellationToken token) {
            foreach (var file in spec.Files) {
                token.ThrowIfCancellationRequested();

                await
                    Downloader.DownloadAsync(GetDlSpec(spec, file, token), token).ConfigureAwait(false);
            }
        }

        protected static MultiMirrorFileDownloadSpec GetDlSpec(FileQueueSpec spec,
            KeyValuePair<FileFetchInfo, ITransferStatus> file) {
            var dlSpec = new MultiMirrorFileDownloadSpec(file.Key.FilePath,
                spec.Location.GetChildFileWithName(file.Key.FilePath)) {
                    Progress = file.Value,
                    Verification = file.Key.OnVerify,
                    ExistingFile =
                        file.Key.ExistingPath != null ? spec.Location.GetChildFileWithName(file.Key.ExistingPath) : null
                };
            dlSpec.LocalFile.MakeSureParentPathExists();
            return dlSpec;
        }

        protected static MultiMirrorFileDownloadSpec GetDlSpec(FileQueueSpec spec,
            KeyValuePair<FileFetchInfo, ITransferStatus> file,
            CancellationToken token) {
            var dlSpec = GetDlSpec(spec, file);
            dlSpec.CancellationToken = token;
            return dlSpec;
        }
    }
}