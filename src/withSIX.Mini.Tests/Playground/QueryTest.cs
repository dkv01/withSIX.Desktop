// <copyright company="SIX Networks GmbH" file="QueryTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using GameServerQuery;
using GameServerQuery.Games.RV;
using GameServerQuery.Parsers;
using NDepend.Helpers;
using NUnit.Framework;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;
using withSIX.Core;
using withSIX.Core.Presentation.Bridge;
using withSIX.Core.Presentation.Bridge.Logging;
using withSIX.Core.Services;
using withSIX.Mini.Presentation.Electron;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Core;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;
using withSIX.Steam.Infra;

namespace withSIX.Mini.Tests.Playground
{
    [TestFixture]
    public class QueryTest
    {
        [Test]
        public async Task Test() {
            var q =
                new SourceMasterQuery(ServerFilterBuilder.Build().FilterByGame("arma3").FilterByDedicated().Value);
            var servers = await q.GetParsedServers(CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine(string.Join(",", servers));
        }

        [Test]
        public async Task Test2() {
            var s = new SteamServiceSessionSignalR();
            await s.Start((uint) SteamGameIds.Arma2Oa, new Uri("http://127.0.0.1:55556")).ConfigureAwait(false);
            var a2 = "arma2arrowpc";
            var a3 = "arma3";
            var filter = ServerFilterBuilder.Build().FilterByAppId((uint) SteamGameIds.Arma3).Value;
            await
                s.GetServers<ArmaServerInfoModel>(
                        new GetServers {Filter = filter, IncludeDetails = true},
                        list => {
                            Console.WriteLine(string.Join(",", list.Select(x => x.QueryEndPoint)));
                        }, CancellationToken.None)
                    .ConfigureAwait(false);
        }

        [Test]
        public async Task Test3() {
            SetupNlog.Initialize("bla");
            var f = new SteamSession.SteamSessionFactory();
            var id = (uint)SteamGameIds.Arma2Oa;
            LockedWrapper.callFactory = new SafeCallFactory(); // workaround for accessviolation errors
            var c = await
                f.Do(id, SteamHelper.Create().SteamPath,
                    async () => {
                        using (var obs2 = new Subject<ArmaServerInfoModel>()) {
                            var s = obs2.Synchronize().Buffer(24)
                                .Do(x => Console.WriteLine("r" + x.ToList<ServerInfoModel>()))
                                .Count().ToTask();
                            var c2 =
                                await
                                    SteamServers.GetServers(f,
                                            ServerFilterBuilder.Build().FilterByAppId(id).FilterByDedicated().Value,
                                            obs2.OnNext)
                                        .ConfigureAwait(false);
                            return new BatchResult(await s);
                        }
                    }).ConfigureAwait(false);
            Console.WriteLine($"C {c}");
        }
    }
}