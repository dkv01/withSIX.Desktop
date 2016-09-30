// <copyright company="SIX Networks GmbH" file="MultiThreadedQueueDownloaderTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Play.Tests.Core.Unit.SyncTests
{
    [TestFixture]
    public class MultiThreadedQueueDownloaderTest : QueueDownloaderTest
    {
        protected override IFileQueueDownloader GetQueueDownloader() => new MultiThreadedFileQueueDownloader(Downloader);

        protected override void VerifyDownloads() {
            VerifyAsyncDownloads();
        }

        [Test]
        public new void CanCancelDownloadFilesAsync() {
            var token = new CancellationTokenSource();
            QDownloader = GetQueueDownloader();
            token.Cancel();

            Func<Task> act = () => QDownloader.DownloadAsync(GetDefaultSpec(), token.Token);

            act.ShouldThrow<OperationCanceledException>();
            token.Dispose();
        }

        [Test]
        public new async Task CanDownloadFilesAsync() {
            QDownloader = GetQueueDownloader();

            await QDownloader.DownloadAsync(GetDefaultSpec());

            VerifyAsyncDownloads();
        }
    }
}