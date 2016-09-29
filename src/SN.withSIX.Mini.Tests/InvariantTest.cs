// <copyright company="SIX Networks GmbH" file="InvariantTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using FluentAssertions;
using NUnit.Framework;
using withSIX.Mini.Core;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Tests
{
    [TestFixture]
    public class InvariantTest
    {
        [SetUp]
        public void Setup() {
            CoreCheat.SetServices(new CoreCheatImpl(new Dummy()));
        }

        [Test]
        public void Test() {
            var c = new ModLocalContent("@testmod", Guid.Empty, "1.0");
            var act2 = new Action(() => c.PackageName = "");
            act2.ShouldThrow<Exception>();
            //var act = new Action(() => c.Name = "");
            //act.ShouldThrow<Exception>();
        }
    }
}