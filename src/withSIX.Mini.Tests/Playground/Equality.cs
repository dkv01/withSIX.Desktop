// <copyright company="SIX Networks GmbH" file="Equality.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Core;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Tests.Playground
{
    [TestFixture]
    public class EqualityTest
    {
        [SetUp]
        public void Setup() {
            CoreCheat.SetServices(new CoreCheatImpl(new Dummy()));
        }

        class TestContentAction : ContentAction<IContent>
        {
            public TestContentAction(IReadOnlyCollection<IContentSpec<IContent>> content,
                CancellationToken cancelToken = new CancellationToken()) : base(content, cancelToken) {}

            public override void Use(IContent content) {
                throw new NotImplementedException();
            }
        }

        class TestClass : Content
        {
            public TestClass(Guid id) {
                Id = id;
            }

            protected override IContentSpec<Content> CreateRelatedSpec(string constraint) {
                throw new NotImplementedException();
            }

            protected override void HandleRelatedContentChildren(ICollection<IContentSpec<Content>> x) {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void ConfirmBaseEntityEquality() {
            var id = Guid.NewGuid();
            var a = new List<KeyValuePair<TestClass, string>>();
            var ac = new TestClass(id);
            var bc = new TestClass(id);
            var cc = new TestClass(Guid.NewGuid());
            a.Add(new KeyValuePair<TestClass, string>(ac, "ac"));
            a.Add(new KeyValuePair<TestClass, string>(bc, "bc"));
            a.Add(new KeyValuePair<TestClass, string>(cc, "cc"));

            a.Should().HaveCount(3);
            a.DistinctBy(x => x.Key).Should().HaveCount(2);
            Action exec = () => a.DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            exec.ShouldNotThrow();
        }

        [Test]
        public void ConfirmContentActionDuplicates() {
            var id = Guid.NewGuid();

            var ac = new TestClass(id);
            var bc = new TestClass(id);
            var cc = new TestClass(Guid.NewGuid());
            var a = new List<KeyValuePair<TestClass, string>>();

            a.Add(new KeyValuePair<TestClass, string>(ac, "ac"));
            a.Add(new KeyValuePair<TestClass, string>(bc, "bc"));
            a.Add(new KeyValuePair<TestClass, string>(cc, "cc"));
            Action act = () => new TestContentAction(a.Select(x => new ContentSpec(x.Key)).ToArray());
            act.ShouldThrow<ArgumentOutOfRangeException>();

            Action act2 =
                () => new TestContentAction(a.Select(x => new ContentSpec(x.Key)).DistinctBy(x => x.Content).ToArray());
            act2.ShouldNotThrow();
        }
    }
}