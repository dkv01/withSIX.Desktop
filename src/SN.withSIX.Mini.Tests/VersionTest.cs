using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using withSIX.Api.Models;

namespace SN.withSIX.Mini.Tests
{
    [TestFixture]
    public class VersionTest
    {
        [Test]
        public void Test() {
            var v = "@ares-1.8.1";
            var sv = new SpecificVersion(v);
            Console.WriteLine(sv);
            Console.WriteLine(sv.VersionData);
        }
    }
}
