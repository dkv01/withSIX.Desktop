// <copyright company="SIX Networks GmbH" file="ListGamesQueryHandlerTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using FakeItEasy.ExtensionSyntax.Full;
using FluentAssertions;
using NUnit.Framework;
using ReactiveUI;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Services;
using withSIX.Play.Applications.DataModels.Games;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Applications.UseCases.Games;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Tests.Presentation.UseCases.Games
{
    [TestFixture]
    public class ListGamesQueryHandlerTest
    {
        [Test]
        public void CreatingWithNullServicesShouldThrow() {
            var act = new Action(() => new ListGamesQueryHandler(null, A.Fake<IGameMapperConfig>()));
            var act2 = new Action(() => new ListGamesQueryHandler(A.Fake<IGameContext>(), null));
            act.ShouldThrow<ArgumentNullException>();
            act2.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void HandleShouldReturnListOfGameDataModels() {
            var gl = A.Fake<IGameContext>();
            var mapper = A.Fake<IGameMapperConfig>();
            var handler = new ListGamesQueryHandler(gl, mapper);

            gl.CallsTo(x => x.Games)
                .Returns(new InMemoryDbSet<Game, Guid>(new ReactiveList<Game>()));

            handler.Handle(new ListGamesQuery());

            A.CallTo(() => mapper.Map<List<GameDataModel>>(A<IOrderedQueryable<Game>>._))
                .MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}