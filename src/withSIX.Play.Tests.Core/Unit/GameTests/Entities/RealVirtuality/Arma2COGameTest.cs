// <copyright company="SIX Networks GmbH" file="Arma2COGameTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NDepend.Path;
using NUnit.Framework;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Entities.RealVirtuality;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Missions;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Games.Services;
using withSIX.Play.Core.Options.Entries;
using withSIX.Play.Tests.Core.Unit.GameTests.Extensions;

namespace withSIX.Play.Tests.Core.Unit.GameTests.Entities.RealVirtuality
{
    [TestFixture, Category("Arma 2 CO")]
    public class Arma2COGameTest
    {
        [SetUp]
        public void SetUp() {
            var gsc = new GameSettingsController();

            _gameo = new Arma2Game(Guid.NewGuid(), gsc);
            _settingso = _gameo.Settings;
            _settingso.Directory = @"C:\temp".ToAbsoluteDirectoryPath();

            _game = new Arma2COGame(Guid.NewGuid(), gsc, _gameo, A.Fake<Arma2FreeGame>());
            _settings = _game.Settings;
            _settings.Directory = @"C:\temp".ToAbsoluteDirectoryPath();
        }

        ArmaSettings _settings;
        Arma2COGame _game;
        ArmaSettings _settingso;
        Arma2Game _gameo;

        [Test]
        public async Task CanLaunch() {
            _game.Settings.LaunchUsingSteam = true;
            var mediator = A.Fake<IRealVirtualityLauncher>();

            var parameters = await mediator.FakeLaunchGameWithSteamLegacy(_game);

            parameters.LaunchExecutable.ToString().Should().Be(@"C:\temp\arma2oa.exe");
            parameters.SteamAppId.Should().Be(33930);
        }

        [Test]
        public async Task CanLaunchWithServerMode() {
            _settings.ServerMode = true;
            var mediator = A.Fake<IRealVirtualityLauncher>();

            var parameters = await mediator.FakeLaunchGame(_game);

            parameters.LaunchExecutable.ToString().Should().Be(@"C:\temp\arma2oaserver.exe");
        }

        [Test]
        public async Task CanQueryServers() {
            var mediator = A.Fake<IGameServerQueryHandler>();
            var info = await mediator.FakeGamespyServerQuery(_game).ConfigureAwait(false);

            info.Tag.Should().Be("arma2oapc");
        }

        [Test]
        public void HasCorrectMetaData() {
            _game.MetaData.Name.Should().Be("ARMA 2: Combined Operations");
            _game.MetaData.Author.Should().Be("Bohemia Interactive");
        }

        [Test]
        public void SupportsCorrectMissionTypes([Values(GameMissionType.Arma2Mission)] GameMissionType type) {
            _game.SupportsContent(new Mission(Guid.Empty) { ContentType = type }).Should().BeTrue();
        }

        [Test]
        public void SupportsCorrectModTypes(
            [Values(GameModType.Arma2CaMod, GameModType.Arma2Mod, GameModType.Arma2OaMod, GameModType.Arma2OaCoMod,
                GameModType.Rv3Mod, GameModType.Rv3MinMod, GameModType.Rv2MinMod, GameModType.RvMinMod)] GameModType
                type) {
                    _game.SupportsContent(new Mod(Guid.Empty) { Type = type }).Should().BeTrue();
        }
    }
}