﻿// <copyright company="SIX Networks GmbH" file="MultiThreadedFileQueueDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Core.Extensions;

namespace withSIX.Sync.Core.Transfer
{
    public class MultiThreadedFileQueueDownloader : FileQueueDownloader
    {
        readonly Func<int> _getMaxThreads;

        public MultiThreadedFileQueueDownloader(IMultiMirrorFileDownloader downloader)
            : this(() => 2, downloader) {}

        public MultiThreadedFileQueueDownloader(Func<int> getMaxThreads, IMultiMirrorFileDownloader downloader)
            : base(downloader) {
            _getMaxThreads = getMaxThreads;
        }

        public override Task DownloadAsync(FileQueueSpec spec) => spec.Files.StartConcurrentTaskQueue(
            file => Downloader.DownloadAsync(GetDlSpec(spec, file)), _getMaxThreads);

        public override Task DownloadAsync(FileQueueSpec spec, CancellationToken token)
            => spec.Files.StartConcurrentTaskQueue(token,
                file => Downloader.DownloadAsync(GetDlSpec(spec, file, token), token), _getMaxThreads);
    }
}