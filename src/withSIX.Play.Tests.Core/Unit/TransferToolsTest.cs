// <copyright company="SIX Networks GmbH" file="TransferToolsTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using FluentAssertions;
using NUnit.Framework;
using Should;
using SN.withSIX.Core;

namespace SN.withSIX.Play.Tests.Core.Unit
{
    [TestFixture]
    public class TransferToolsTest
    {
        [SetUp]
        public void SetUp() {
            _tools = new Tools.TransferTools();
        }

        private Tools.TransferTools _tools;
        private static readonly Uri baseUri = new Uri("http://cdn.withsix.com/some/path");

        private string TestEncode(string path) => _tools.EncodePathIfRequired(baseUri, path);

        private void ConfirmEncode(string inputPath, string outputPath) {
            TestEncode(inputPath).Should().Be(outputPath);
        }

        [Test]
        public void Test() {
            Action act = () => TestEncode(@"packages\@testpackage.json");
            act.ShouldThrow<NotSupportedException>();

            ConfirmEncode("some-file-with-#-yay.gz", "some-file-with-%23-yay.gz");
            ConfirmEncode("some-file-with- -yay.gz", "some-file-with-%20-yay.gz");
            ConfirmEncode("some-file-with-%20-yay.gz", "some-file-with-%2520-yay.gz");
            ConfirmEncode("packages/@cba-1.2.3.json", "packages/@cba-1.2.3.json");
        }
    }
}