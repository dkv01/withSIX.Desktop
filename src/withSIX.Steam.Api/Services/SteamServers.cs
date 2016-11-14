// <copyright company="SIX Networks GmbH" file="SteamServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GameServerQuery;
using GameServerQuery.Games.RV;
using GameServerQuery.Parsers;
using NDepend.Helpers;
using withSIX.Api.Models.Extensions;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Api.Services
{
    public static class SteamServers
    {
        public static Task<int> GetServers(ISteamSessionLocator locator, bool inclRules,
            List<Tuple<string, string>> filter, Action<ArmaServerInfoModel> act) {
            return HandleAlt(locator, filter, act); // HandleNative(locator, inclRules, filter, act)
        }

        private static async Task<int> HandleNative(ISteamSessionLocator locator, bool inclRules,
            List<Tuple<string, string>> filter, Action<ArmaServerInfoModel> act) {
            var api = new SteamApi(locator);

            var degreeOfParallelism = inclRules ? 30 : 1;
            using (var bc = new BlockingCollection<ArmaServerInfoModel>()) {
                // TODO: better MT model
                var bcT = TaskExt.StartLongRunningTask(async () => {
                    await Task.WhenAll(Enumerable.Range(1, degreeOfParallelism).Select(_ =>
                            Task.Run(async () => {
                                foreach (var s in bc.GetConsumingEnumerable()) {
                                    await UpdateServerInfo(s, api, inclRules).ConfigureAwait(false);
                                    act(s);
                                }
                            })
                    ));
                });
                var c2 = await api.GetServerInfo(locator.Session.AppId, x => {
                    try {
                        var ip = x.m_NetAdr.GetQueryAddressString().Split(':').First();
                        var ipAddress = IPAddress.Parse(ip);
                        var map = x.GetMap();
                        var s = new ArmaServerInfoModel(new IPEndPoint(ipAddress, x.m_NetAdr.GetQueryPort())) {
                            ConnectionEndPoint = new IPEndPoint(ipAddress, x.m_NetAdr.GetConnectionPort()),
                            Name = x.GetServerName(),
                            Tags = x.GetGameTags(),
                            Mission = string.IsNullOrEmpty(map) ? null : x.GetGameDescription(),
                            Map = map,
                            Ping = x.m_nPing,
                            MaxPlayers = x.m_nMaxPlayers,
                            CurrentPlayers = x.m_nPlayers,
                            RequirePassword = x.m_bPassword,
                            IsVacEnabled = x.m_bSecure,
                            ServerVersion = x.m_nServerVersion
                        };
                        bc.Add(s);
                    } catch (Exception ex) {
                        Console.WriteLine(ex);
                    }
                }, filter);
                bc.CompleteAdding();
                await bcT;
                return c2;
            }
        }

        private static async Task<int> HandleAlt(ISteamSessionLocator locator, List<Tuple<string, string>> filter,
            Action<ArmaServerInfoModel> act) {
            var api = new SteamApi(locator);

            var bc = new Subject<IPEndPoint>();

            var dq = new DirectQuerier();
            var t = dq.Load(page => {
                foreach (var i in page) {
                    var r = (SourceParseResult) i.Settings;
                    var s = r.MapTo<ArmaServerInfoModel>();
                    s.Ping = i.Ping;
                    if (r.Rules != null) {
                        try {
                            var rules = SourceQueryParser.ParseRules(r.Rules);
                            var mods = SourceQueryParser.GetList(rules, "modNames");
                            s.SignatureList = SourceQueryParser.GetList(rules, "sigNames").ToHashSet();
                            s.ModList = mods.Select(x => new ServerModInfo {Name = x}).ToList();
                            s.ReceivedRules = true;
                        } catch (Exception ex) {
                            Console.WriteLine(ex);
                        }
                    }
                    act(s);
                }
            }, bc);

            var c2 = await api.GetServerInfo(locator.Session.AppId, x => {
                try {
                    var ip = x.m_NetAdr.GetQueryAddressString().Split(':').First();
                    var ipAddress = IPAddress.Parse(ip);
                    bc.OnNext(new IPEndPoint(ipAddress, x.m_NetAdr.GetQueryPort()));
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            }, filter).ConfigureAwait(false);
            bc.OnCompleted();
            await t;
            return c2;
        }

        private static async Task UpdateServerInfo(ArmaServerInfoModel s, SteamApi api, bool inclRules) {
            s.GameTags = s.Tags == null ? null : GameTags.Parse(s.Tags);
            if (inclRules) {
                var rules = await api.GetServerRules(s.QueryEndPoint).ConfigureAwait(false);
                var mods = SourceQueryParser.GetList(rules, "modNames");
                s.SignatureList = SourceQueryParser.GetList(rules, "sigNames").ToHashSet();
                s.ModList = mods.Select(x => new ServerModInfo {Name = x}).ToList();
            }
        }

        public class DirectQuerier
        {
            internal async Task Load(Action<IList<ServerQueryResult>> onNext, IObservable<IPEndPoint> masterObservable) {
                var rs = new ReactiveSource();
                using (var udpClient = CreateUdpClient()) {
                    var directObs = rs.GetResults(
                            masterObservable,
                            udpClient, new QuerySettings {
                                DegreeOfParallelism = 40,
                                InclRules = true
                            });
                    await
                        rs.ProcessResults(directObs.ObserveOn(TaskPoolScheduler.Default)).Buffer(231)
                            .Do(onNext);
                }
            }

            private static UdpClient CreateUdpClient() {
                var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                udpClient.Client.ReceiveBufferSize = 25 * 1024 * 1024;
                //udpClient.Ttl = 255;
                return udpClient;
            }
        }
    }
}