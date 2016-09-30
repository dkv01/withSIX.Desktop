// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Entities.RealVirtuality;
using withSIX.Play.Core.Games.Legacy.ServerQuery;
using withSIX.Play.Core.Games.Services;
using withSIX.Play.Core.Games.Services.GameLauncher;

namespace withSIX.Play.Tests.Core.Unit.GameTests.Extensions
{
    public static class MediatorExtensions
    {
        public static T RequestFaked<T, T2>(this IMediator mediator, Action act) where T : class, IRequest<T2> {
            T r = null;
            A.CallTo(() => mediator.Send(A<IRequest<T2>>._))
                .Invokes((IRequest<T2> x) => r = (T) x);
            act();
            return r;
        }

        public static async Task<T> RequestAsyncFaked<T, T2>(this IMediator mediator, Func<Task> act)
            where T : class, IAsyncRequest<T2> {
            T r = null;
            A.CallTo(() => mediator.SendAsync(A<IAsyncRequest<T2>>._))
                .Invokes((IAsyncRequest<T2> x) => r = (T) x)
                .ReturnsLazily(() => Task.FromResult(default(T2)));
            await act().ConfigureAwait(false);
            return r;
        }

        public static async Task<LaunchGameWithSteamInfo> FakeLaunchGameWithSteam(this ILaunchWithSteam mediator,
            RealVirtualityGame game) {
            LaunchGameWithSteamInfo parameters = null;
            A.CallTo(() => mediator.Launch(A<LaunchGameWithSteamInfo>._))
                .Invokes((LaunchGameWithSteamInfo x) => parameters = x)
                .ReturnsLazily(() => Task.FromResult(default(Process)));
            await game.Launch((dynamic) mediator);
            return parameters;
        }

        public static async Task<LaunchGameWithSteamLegacyInfo> FakeLaunchGameWithSteamLegacy(
            this ILaunchWithSteamLegacy mediator, RealVirtualityGame game) {
            LaunchGameWithSteamLegacyInfo parameters = null;
            A.CallTo(() => mediator.Launch(A<LaunchGameWithSteamLegacyInfo>._))
                .Invokes((LaunchGameWithSteamLegacyInfo x) => parameters = x)
                .ReturnsLazily(() => Task.FromResult(default(Process)));
            await game.Launch((dynamic) mediator);
            return parameters;
        }

        public static async Task<LaunchGameInfo> FakeLaunchGame(this ILaunch mediator, Game game) {
            LaunchGameInfo parameters = null;
            A.CallTo(() => mediator.Launch(A<LaunchGameInfo>._))
                .Invokes((LaunchGameInfo x) => parameters = x)
                .ReturnsLazily(() => Task.FromResult(default(Process)));
            await game.Launch((dynamic) mediator);
            return parameters;
        }

        public static async Task<GamespyServersQuery> FakeGamespyServerQuery(this IGameServerQueryHandler mediator,
            ISupportServers game) {
            GamespyServersQuery parameters = null;
            A.CallTo(() => mediator.Query(A<GamespyServersQuery>._))
                .Invokes((GamespyServersQuery x) => parameters = x)
                .ReturnsLazily(() => Task.FromResult(default(IEnumerable<ServerQueryResult>)));
            await game.QueryServers(mediator);
            return parameters;
        }

        public static async Task<SourceServersQuery> FakeSourceServerQuery(this IGameServerQueryHandler mediator,
            ISupportServers game) {
            SourceServersQuery parameters = null;
            A.CallTo(() => mediator.Query(A<SourceServersQuery>._))
                .Invokes((SourceServersQuery x) => parameters = x)
                .ReturnsLazily(() => Task.FromResult(default(IEnumerable<ServerQueryResult>)));
            await game.QueryServers(mediator);
            return parameters;
        }
    }
}