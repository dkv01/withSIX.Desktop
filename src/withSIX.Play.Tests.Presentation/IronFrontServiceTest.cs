// <copyright company="SIX Networks GmbH" file="IronFrontServiceTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using FakeItEasy.ExtensionSyntax.Full;
using FluentAssertions;
using NUnit.Framework;
using withSIX.Core;
using withSIX.Core.Infra.Services;
using withSIX.Play.Applications.Services;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Arma;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Api.Models.Games;

namespace withSIX.Play.Tests.Presentation
{
    [TestFixture, Ignore("")]
    public class IronFrontServiceTest
    {
        [SetUp]
        public void SetUp() {
            _gameContext = CreateFakeGameList();
            _installer = A.Fake<IronFrontInstaller>();
            _service = CreateIFService();
        }

        IronFrontService CreateIFService() => new IronFrontService(_installer, CreateFakeGameList(), new RepoActionHandler(new BusyStateHandler()));

        Collection GetFakeModSet() {
            var modSetFake = A.Fake<Collection>();
            modSetFake.CallsTo(modSet => modSet.EnabledMods).Returns(CreateIModList("@IF", "@IFA3"));
            return modSetFake;
        }

        Mod[] CreateIModList(params string[] modNames) => modNames.Select(GetFakeMod).ToArray();

        Mod GetFakeMod(string modName) {
            var fakeIfMod = A.Fake<Mod>();
            fakeIfMod.Name = modName;
            return fakeIfMod;
        }

        IGameContext CreateFakeGameList() {
            var gameList = A.Fake<IGameContext>();
            var list = new List<Game> {CreateFakeGame(GameIds.IronFront), CreateFakeGame(GameIds.Arma2Oa)};
            /*
            list.Add(CreateFakeGame(GameGuids.Arma1Uuid));
            list.Add(CreateFakeGame(GameGuids.Arma2FreeUuid));
            list.Add(CreateFakeGame(GameGuids.Arma2Uuid));
            list.Add(CreateFakeGame(GameGuids.Arma3Uuid));
            list.Add(CreateFakeGame(GameGuids.DayZSAUuid));
            list.Add(CreateFakeGame(GameGuids.TKOHUuid));
             */

            var dbSet = A.Fake<InMemoryDbSet<Game, Guid>>(o => o.WithArgumentsForConstructor(new Object[] {list}));
            gameList.CallsTo(gl => gl.Games).Returns(dbSet);

            return gameList;
        }

        Game CreateFakeGame(string gameUUid) {
            var game = A.Fake<Game>();
            //game.Id = Guid.Parse(gameUUid);
            return game;
        }

        IGameContext _gameContext;
        IronFrontService _service;
        IronFrontInstaller _installer;

        [Test]
        public void IsIronFrontEnabled_ContainsMods() {
            var modSetFake = GetFakeModSet();
            _service.IsIronFrontEnabled(modSetFake).Should().BeTrue();
        }

/*
        [Test]
        public void IsIronFrontEnabled_IsModset() {
            var modSetFake = A.Fake<Collection>();
            modSetFake.Uuid = "if_a2";
            _service.IsIronFrontEnabled(modSetFake).Should().BeTrue();
        }
*/

        [Test]
        public void IsIronFrontEnabled_NotNull() {
            _service.IsIronFrontEnabled(null).Should().BeFalse();
        }
    }
}