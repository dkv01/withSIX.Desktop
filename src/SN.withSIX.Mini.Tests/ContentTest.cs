// <copyright company="SIX Networks GmbH" file="ContentTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using NUnit.Framework;
using Should;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Core;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Data.Services;
using withSIX.Api.Models.Games;

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
            var mod1 = new ModNetworkContent("@test-mod-1", GameGuids.Arma3);
            var mod2 = new ModNetworkContent("@test-mod-2", GameGuids.Arma3);
            mod1.Dependencies.Add(new NetworkContentSpec(mod2, "1.0.0"));

            mod1.GetRelatedContent().First().Constraint.ShouldEqual("1.0.0");
        }
    }

    [TestFixture]
    public class CollectionContentTest : TestBase
    {
        [TestCase]
        public void ConfirmDependencyConstraints() {
            var mod1 = new ModNetworkContent("@test-mod-1", GameGuids.Arma3);
            var mod2 = new ModNetworkContent("@test-mod-2", GameGuids.Arma3);
            mod1.Dependencies.Add(new NetworkContentSpec(mod2, "1.0.0"));

            var collection = new LocalCollection(GameGuids.Arma3, "Test collection", new[] {
                new ContentSpec(mod1),
                new ContentSpec(mod2)
            });

            collection.GetRelatedContent().First().Constraint.ShouldEqual("1.0.0");
        }

        [TestCase]
        public void ConfirmDependencyConstraintsOverride() {
            var mod1 = new ModNetworkContent("@test-mod-1", GameGuids.Arma3);
            var mod2 = new ModNetworkContent("@test-mod-2", GameGuids.Arma3);
            mod1.Dependencies.Add(new NetworkContentSpec(mod2, "1.0.0"));

            var collection = new LocalCollection(GameGuids.Arma3, "Test collection", new[] {
                new ContentSpec(mod1),
                new ContentSpec(mod2, "2.0.0")
            });

            collection.GetRelatedContent().First().Constraint.ShouldEqual("2.0.0");
        }
    }
}