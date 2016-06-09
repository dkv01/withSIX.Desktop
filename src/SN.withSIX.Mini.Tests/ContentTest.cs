using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Should;
using SN.withSIX.Core;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Core;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Data.Services;

namespace SN.withSIX.Mini.Tests
{

    class Dummy : IDomainEventHandlerGrabber
    {
        public IDomainEventHandler Get() => new DefaultDomainEventHandler();
        public IDomainEventHandler GetSettings() => new DefaultDomainEventHandler();
    }

    [TestFixture]
    public abstract class TestBase
    {
        [SetUp]
        public void Setup() {
            CoreCheat.SetServices(new CoreCheatImpl(new Dummy()));
        }
        
    }

    [TestFixture]
    public class ContentTest : TestBase
    {
        [TestCase]
        public void ConfirmDependencyConstraints() {
            var mod1 = new ModNetworkContent("Test mod 1", "@test-mod-1", GameUuids.Arma3);
            var mod2 = new ModNetworkContent("Test mod 2", "@test-mod-2", GameUuids.Arma3);
            mod1.Dependencies.Add(new NetworkContentSpec(mod2, "1.0.0"));

            mod1.GetRelatedContent().First().Constraint.ShouldEqual("1.0.0");
        }
    }

    [TestFixture]
    public class CollectionContentTest : TestBase
    {
        [TestCase]
        public void ConfirmDependencyConstraints() {
            var mod1 = new ModNetworkContent("Test mod 1", "@test-mod-1", GameUuids.Arma3);
            var mod2 = new ModNetworkContent("Test mod 2", "@test-mod-2", GameUuids.Arma3);
            mod1.Dependencies.Add(new NetworkContentSpec(mod2, "1.0.0"));

            var collection = new LocalCollection(GameUuids.Arma3, "Test collection", new[] {
                new ContentSpec(mod1),
                new ContentSpec(mod2)
            });

            collection.GetRelatedContent().First().Constraint.ShouldEqual("1.0.0");
        }

        [TestCase]
        public void ConfirmDependencyConstraintsOverride() {
            var mod1 = new ModNetworkContent("Test mod 1", "@test-mod-1", GameUuids.Arma3);
            var mod2 = new ModNetworkContent("Test mod 2", "@test-mod-2", GameUuids.Arma3);
            mod1.Dependencies.Add(new NetworkContentSpec(mod2, "1.0.0"));

            var collection = new LocalCollection(GameUuids.Arma3, "Test collection", new [] {
                new ContentSpec(mod1), 
                new ContentSpec(mod2, "2.0.0")
            });

            collection.GetRelatedContent().First().Constraint.ShouldEqual("2.0.0");
        }
    }
}
