// <copyright company="SIX Networks GmbH" file="ServersTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NDepend.Path;
using NUnit.Framework;
using SteamLayerWrap;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Plugin.Arma;
using withSIX.Steam.Presentation;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;
using LoggingSetup = withSIX.Mini.Presentation.Electron.LoggingSetup;

namespace withSIX.Mini.Tests.Arma
{
    [TestFixture]
    public class ServersTest
    {
        private static async Task<IList<ArmaServerInfo>> PerformAction(ISteamApi steamApi, ServerFilterWrap filter) {
            using (var sb = await SteamActions.CreateServerBrowser(steamApi).ConfigureAwait(false)) {
                using (var cts = new CancellationTokenSource()) {
                    var obs =
                        await
                            sb.GetServersInclDetails(cts.Token, filter, true)
                                .ConfigureAwait(false);
                    var s = await obs.Take(10).ToList();
                    cts.Cancel();
                    return s;
                }
            }
        }

        [Test]
        public async Task GetServers() {
            LoggingSetup.Setup("Tests");
            LockedWrapper.callFactory = new SafeCallFactory(); // workaround for accessviolation errors
            var serverFilterBuilder = ServerFilterBuilder.Build();
            serverFilterBuilder.FilterByAddresses(new[] {
                new IPEndPoint(IPAddress.Parse("5.189.150.147"), 2302),
                new IPEndPoint(IPAddress.Parse("5.189.136.56"), 2302),
                new IPEndPoint(IPAddress.Parse("80.241.208.192"), 2302)
            });
            var s = await
                SteamActions.PerformArmaSteamAction(
                    steamApi => PerformAction(steamApi, serverFilterBuilder.Value), (uint) SteamGameIds.Arma3,
                    new SteamSession.SteamSessionFactory()).ConfigureAwait(false);
            s.Count.Should().Be(3);
            var json = s.ToJson(true);
            Console.WriteLine(json);
            @"C:\temp\crawl\arma-servers.json".ToAbsoluteFilePath().WriteText(json);
        }
    }
}