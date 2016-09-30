// <copyright company="SIX Networks GmbH" file="ZsyncDownloadProtocolTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NDepend.Path;
using NUnit.Framework;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.Protocols;
using withSIX.Sync.Core.Transfer.Protocols.Handlers;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Play.Tests.Core.Unit.SyncTests.Protocols
{
    [TestFixture]
    public class ZsyncDownloadProtocolTest : DownloadProtocolTest
    {
        [SetUp]
        public void SetUp() {
            _zsyncLauncher = A.Fake<IZsyncLauncher>();
            Strategy = new ZsyncDownloadProtocol(_zsyncLauncher);
        }

        IZsyncLauncher _zsyncLauncher;

        [Test]
        public void CanDownloadZsync() {
            Strategy.Download(new FileDownloadSpec(new Uri("zsync://host/c"), "c".ToAbsoluteFilePath()));

            A.CallTo(() => _zsyncLauncher.RunAndProcess(new ZsyncParams(A<ITransferProgress>._, new Uri("http://host/c.zsync"), "C://c".ToAbsoluteFilePath())))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public async Task CanDownloadZsyncAsync() {
            await Strategy.DownloadAsync(new FileDownloadSpec(new Uri("zsync://host/c"), "C://c"));

            A.CallTo(
                () => _zsyncLauncher.RunAndProcessAsync(new ZsyncParams(A<ITransferProgress>._, new Uri("http://host/c.zsync"), "C://c".ToAbsoluteFilePath())))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public async Task CanDownloadZsyncAsyncWithProgress() {
            var progress = A.Fake<ITransferProgress>();

            await
                Strategy.DownloadAsync(new FileDownloadSpec(new Uri("zsync://host/c"), "c:\\a".ToAbsoluteFilePath(), progress)).ConfigureAwait(false);

            A.CallTo(() => _zsyncLauncher.RunAndProcessAsync(new ZsyncParams(progress, new Uri("http://host/c.zsync"), "C:\\c".ToAbsoluteFilePath())))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void ThrowsOnProtocolMismatch() {
            Action act = () => Strategy.Download(new FileDownloadSpec("someprotocol://host/a", "C:\\a"));

            act.ShouldThrow<ProtocolMismatch>();
        }
    }
}