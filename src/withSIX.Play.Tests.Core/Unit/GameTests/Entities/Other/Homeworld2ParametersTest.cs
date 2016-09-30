// <copyright company="SIX Networks GmbH" file="Homeworld2ParametersTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using FluentAssertions;
using NUnit.Framework;
using withSIX.Play.Core.Games.Entities.Other;

namespace withSIX.Play.Tests.Core.Unit.GameTests.Entities.Other
{
    [TestFixture, Category("Homeworld 2")]
    public class Homeworld2ParametersTest
    {
        static Homeworld2StartupParameters GetClass(string parameters = "") => new Homeworld2StartupParameters(parameters);

        [Test]
        public void CanCreateClass() {
            var parameters = GetClass();
            parameters.Get().Should().HaveCount(0);
        }
    }
}