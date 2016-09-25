// <copyright company="SIX Networks GmbH" file="ServersTest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using NUnit.Framework;
using SN.withSIX.Mini.Plugin.Arma.Steam;
using SteamLayerWrap;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Tests.Arma
{
    [TestFixture]
    public class ServersTest
    {
        [Test]
        public async Task GetServers() {
            await SBTest().ConfigureAwait(false);
        }

        private static async Task SBTest() {
            using (var wrap = new SteamAPIWrap()) {
                var steamApi = new SteamApi(wrap);
                await steamApi.Initialize(@"C:\projects\arma3\launcher-bin".ToAbsoluteDirectoryPath())
                        .ConfigureAwait(false);
                var sb = new ServerBrowser(steamApi);
                var dict = new Dictionary<IPEndPoint, ArmaServerInfo>();
                var obs = sb.ServerResponses
                    .Where(x => x.ServerInfo != null)
                    .Select(x => ArmaServerInfo.FromWrap(x.ServerIndex, x.ServerInfo))
                    .Do(async si => {
                        dict.Add(si.QueryEndPoint, si);
                        //Console.Write($"ServerInfo: {si.ToJson()}");
                        try {
                            var r = await sb.GetServerRules(si.QueryEndPoint).ConfigureAwait(false);
                            si.ApplyServerDataToServerInfo(r);
                            Console.Write($"ServerInfo: {si.ToJson()}");
                        } catch (Exception ex) {}
                    })
                    .TakeUntil(sb.ServerResponses.Throttle(TimeSpan.FromSeconds(10)));
                // TODO: Wait for the Rules to complete
                sb.GetServerInfoInclDetails(ServerFilterBuilder
                    .Build()
                    .Value, CancellationToken.None);

                await obs;
                @"C:\temp\crawl\arma-servers.json".ToAbsoluteFilePath().WriteText(dict.ToJson(true));
            }
        }
    }
}