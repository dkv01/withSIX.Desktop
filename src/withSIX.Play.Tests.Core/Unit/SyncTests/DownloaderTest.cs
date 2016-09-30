// <copyright company="SIX Networks GmbH" file="DownloaderTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using FakeItEasy;
using FluentAssertions;
using NDepend.Path;
using NUnit.Framework;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.Protocols;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Play.Tests.Core.Unit.SyncTests
{
    [TestFixture]
    public class DownloaderTest
    {
        [SetUp]
        public void Setup() {
            _strategy = A.Fake<IDownloadProtocol>();
            A.CallTo(() => _strategy.Schemes)
                .Returns(new[] {"http"});

            var authProvider = A.Fake<IAuthProvider>();
            A.CallTo(() => authProvider.HandleUri(new Uri(TestUrl)))
                .Returns(new Uri(TestUrl));
            _downloader = new FileDownloader(new[] {_strategy}, authProvider);
        }

        FileDownloader _downloader;
        IDownloadProtocol _strategy;
        const string TestUrl = "http://a";

        static FileDownloadSpec GetDownloadSpec(string remoteFile, IAbsoluteFilePath localFile,
ITransferProgress progress) => A<FileDownloadSpec>.That.Matches(
        x => x.LocalFile == localFile && x.Uri == new Uri(remoteFile) && x.Progress == progress);

        static FileDownloadSpec GetDownloadSpec(string remoteFile, IAbsoluteFilePath localFile) => A<FileDownloadSpec>.That.Matches(
        x => x.LocalFile == localFile && x.Uri == new Uri(remoteFile));

        [Test]
        public void CanDownloadAsyncFromRegisteredProtocol() {
            _downloader.DownloadAsync(TestUrl, "C:\\a".ToAbsoluteFilePath());

            A.CallTo(() => _strategy.DownloadAsync(GetDownloadSpec(TestUrl, "C:\\a".ToAbsoluteFilePath())))
                .MustHaveHappened(Repeated.Exactly.Once);
        }


        [Test]
        public void CanDownloadAsyncFromRegisteredProtocolWithProgress() {
            var progress = A.Fake<ITransferProgress>();

            _downloader.DownloadAsync(TestUrl, "C:\\a".ToAbsoluteFilePath(), progress);

            A.CallTo(() => _strategy.DownloadAsync(GetDownloadSpec(TestUrl, "C:\\a".ToAbsoluteFilePath(), progress)))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void CanDownloadFromRegisteredProtocol() {
            _downloader.Download(TestUrl, "C:\\a".ToAbsoluteFilePath());

            A.CallTo(() => _strategy.Download(GetDownloadSpec(TestUrl, "C:\\a".ToAbsoluteFilePath())))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void CanDownloadFromRegisteredProtocolWithProgress() {
            var progress = A.Fake<ITransferProgress>();

            _downloader.Download(TestUrl, "C:\\a".ToAbsoluteFilePath(), progress);

            A.CallTo(() => _strategy.Download(GetDownloadSpec(TestUrl, "C:\\a".ToAbsoluteFilePath(), progress)))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void ThrowsOnUnknownScheme() {
            Action act = () => _downloader.Download("rsync://a", "C:\\a".ToAbsoluteFilePath());

            act.ShouldThrow<ProtocolNotSupported>();
        }
    }
}