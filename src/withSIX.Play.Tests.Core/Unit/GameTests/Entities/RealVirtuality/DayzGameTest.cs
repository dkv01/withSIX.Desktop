// <copyright company="SIX Networks GmbH" file="DayzGameTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NDepend.Path;
using NUnit.Framework;
using withSIX.Play.Core.Games.Entities.RealVirtuality;
using withSIX.Play.Core.Games.Services;
using withSIX.Play.Core.Options.Entries;
using withSIX.Play.Tests.Core.Unit.GameTests.Extensions;

namespace withSIX.Play.Tests.Core.Unit.GameTests.Entities.RealVirtuality
{
    [TestFixture, Category("DayZ")]
    public class DayZGameTest
    {
        [SetUp]
        public void SetUp() {
            _game = new DayZGame(Guid.NewGuid(), new GameSettingsController());
            _settings = _game.Settings;
            _settings.Directory = @"C:\temp".ToAbsoluteDirectoryPath();
        }

        DayZSettings _settings;
        DayZGame _game;

        [Test]
        public async Task CanLaunch() {
            var mediator = A.Fake<IRealVirtualityLauncher>();
            var parameters = await mediator.FakeLaunchGameWithSteam(_game);
            parameters.LaunchExecutable.ToString().Should().Be(@"C:\temp\dayz.exe");
            parameters.SteamAppId.Should().Be(107410);
        }

        [Test]
        public async Task CanQueryServers() {
            var mediator = A.Fake<IGameServerQueryHandler>();
            var info = await mediator.FakeSourceServerQuery(_game).ConfigureAwait(false);
            info.Tag.Should().Be("dayz");
        }

        [Test]
        public void HasCorrectMetaData() {
            _game.MetaData.Name.Should().Be("DayZ");
            _game.MetaData.Author.Should().Be("Bohemia Interactive");
        }
    }
}