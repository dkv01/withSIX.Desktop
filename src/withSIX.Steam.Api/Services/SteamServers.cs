// <copyright company="SIX Networks GmbH" file="SteamServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GameServerQuery.Games.RV;
using GameServerQuery.Parsers;
using NDepend.Helpers;
using withSIX.Api.Models.Extensions;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Api.Services
{
    public static class SteamServers
    {
        public static async Task<int> GetServers(ISteamSessionLocator locator, List<Tuple<string, string>> filter, Action<ArmaServerInfoModel> act) {
            var api = new SteamApi(locator);
            using (var bc = new BlockingCollection<ArmaServerInfoModel>()) {
                // TODO: better MT model
                var bcT = TaskExt.StartLongRunningTask(async () => {
                    await Task.WhenAll(Enumerable.Range(1, 10).Select(_ =>
                            Task.Run(async () => {
                                foreach (var s in bc.GetConsumingEnumerable()) {
                                    await UpdateServerInfo(s, api).ConfigureAwait(false);
                                    act(s);
                                }
                            })
                    ));
                });
                var c2 = await api.GetServerInfo(locator.Session.AppId, x => {
                    var ip = x.m_NetAdr.GetQueryAddressString().Split(':').First();
                    var ipAddress = IPAddress.Parse(ip);
                    var s = new ArmaServerInfoModel(new IPEndPoint(ipAddress, x.m_NetAdr.GetQueryPort())) {
                        ConnectionEndPoint = new IPEndPoint(ipAddress, x.m_NetAdr.GetConnectionPort()),
                        Name = x.GetServerName(),
                        Tags = x.GetGameTags(),
                        Mission = x.GetGameDescription(),
                        Map = x.GetMap(),
                        Ping = x.m_nPing,
                        MaxPlayers = x.m_nMaxPlayers,
                        CurrentPlayers = x.m_nPlayers,
                        RequirePassword = x.m_bPassword,
                        IsVacEnabled = x.m_bSecure,
                        ServerVersion = x.m_nServerVersion
                    };
                    bc.Add(s);
                }, filter);
                bc.CompleteAdding();
                await bcT;
                return c2;
            }
        }

        private static async Task UpdateServerInfo(ArmaServerInfoModel s, SteamApi api) {
            s.GameTags = s.Tags == null ? null : GameTags.Parse(s.Tags);
            var rules = await api.GetServerRules(s.QueryEndPoint).ConfigureAwait(false);
            var mods = SourceQueryParser.GetList(rules, "modNames");
            s.SignatureList = SourceQueryParser.GetList(rules, "sigNames").ToHashSet();
            s.ModList = mods.Select(x => new ServerModInfo {Name = x}).ToList();
        }
    }
}