using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;

namespace SN.withSIX.Play.Tests.Core
{
    [TestFixture]
    public abstract class BaseTest
    {
        [TearDown]
        public void BaseTearDown() {
            ExceptionTest = null;
        }

        protected Fixture Fixture { get; } = new Fixture();
        protected Action ExceptionTest { get; set; }

        protected BaseTest() {
            Fixture.Customize(new AutoFakeItEasyCustomization());
        }
    }

    public abstract class BaseTest<T> : BaseTest
    {
        // ReSharper disable once InconsistentNaming
        public T SUT { get; set; }

        protected virtual void BuildFixture() => SUT = Fixture.Create<T>();
    }
}
