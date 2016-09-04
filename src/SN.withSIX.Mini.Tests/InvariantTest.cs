using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SN.withSIX.Mini.Core;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Data.Services;

namespace SN.withSIX.Mini.Tests
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
