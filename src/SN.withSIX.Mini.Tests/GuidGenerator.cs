// <copyright company="SIX Networks GmbH" file="GuidGenerator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using NUnit.Framework;
using SN.withSIX.Mini.Core.Extensions;

namespace SN.withSIX.Mini.Tests
{
    [TestFixture]
    public class GuidGenerator
    {
        [Test]
        public void GenerateIds() {
            Console.WriteLine("Starbound: " + global::withSIX.Api.Models.Extensions.GameExtensions.CreateSteamContentIdGuid(211820));
            Console.WriteLine("Stellaris: " + global::withSIX.Api.Models.Extensions.GameExtensions.CreateSteamContentIdGuid(281990));
        }
    }
}