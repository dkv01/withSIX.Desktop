// <copyright company="SIX Networks GmbH" file="QueueDownloaderTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NDepend.Path;
using NUnit.Framework;
using withSIX.Sync.Core.Legacy;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Play.Tests.Core.Unit.SyncTests
{
    [TestFixture]
    public class QueueDownloaderTest
    {
        [SetUp]
        public void SetUp() {
            Downloader = A.Fake<IMultiMirrorFileDownloader>();
        }

        protected IMultiMirrorFileDownloader Downloader;
        protected IFileQueueDownloader QDownloader;

        static IDictionary<FileFetchInfo, ITransferStatus> GetDefaultDownloads() => new[] {"C:\\a", "c:\\b"}.Select(
            x =>
                new KeyValuePair<FileFetchInfo, ITransferStatus>(
                    new FileFetchInfo(x), new TransferStatus(x)))
            .ToDictionary(x => x.Key, x => x.Value);

        protected static FileQueueSpec GetDefaultSpec() => new FileQueueSpec(GetDefaultDownloads(), @"C:\temp".ToAbsoluteDirectoryPath());

        static MultiMirrorFileDownloadSpec GetDownloadSpec(string remoteFile) => A<MultiMirrorFileDownloadSpec>.That.Matches(x => x.RemoteFile == remoteFile);

        protected virtual void VerifyDownloads() {
            A.CallTo(() => Downloader.Download(GetDownloadSpec("C:\\a")))
                .MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => Downloader.Download(GetDownloadSpec("C:\\b")))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        protected void VerifyAsyncDownloads() {
            A.CallTo(() => Downloader.DownloadAsync(GetDownloadSpec("C:\\a")))
                .MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => Downloader.DownloadAsync(GetDownloadSpec("C:\\b")))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        protected virtual IFileQueueDownloader GetQueueDownloader() => new FileQueueDownloader(Downloader);

        [Test]
        public void CanCancelDownloadFilesAsync() {
            var token = new CancellationTokenSource();
            QDownloader = GetQueueDownloader();
            token.Cancel();

            Func<Task> act = () => QDownloader.DownloadAsync(GetDefaultSpec(), token.Token);

            act.ShouldThrow<OperationCanceledException>();
            token.Dispose();
        }

        [Test]
        public async Task CanDownloadFilesAsync() {
            QDownloader = GetQueueDownloader();

            await QDownloader.DownloadAsync(GetDefaultSpec());

            VerifyAsyncDownloads();
        }
    }
}