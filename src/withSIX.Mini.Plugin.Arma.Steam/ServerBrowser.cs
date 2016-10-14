// <copyright company="SIX Networks GmbH" file="ServerBrowser.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery;
using GameServerQuery.Games.RV;
using SteamLayerWrap;
using withSIX.Core.Applications.Extensions;
using withSIX.Mini.Plugin.Arma.Models;
using withSIX.Mini.Plugin.Arma.Services;
using ServerModInfo = SteamLayerWrap.ServerModInfo;

namespace withSIX.Steam.Plugin.Arma
{
    public class ServerBrowser : ServerInfoFetcher
    {
        private readonly Func<IPEndPoint, Task<ServerInfoRulesFetcher>> _fetcherFact;

        public ServerBrowser(LockedWrapper<MatchmakingServiceWrap> api,
            Func<IPEndPoint, Task<ServerInfoRulesFetcher>> fetcherFact) : base(api) {
            _fetcherFact = fetcherFact; 
        }

        public async Task<IObservable<ArmaServerInfo>> GetServers(CancellationToken ct, ServerFilterWrap filter) {
            var dsp = new CompositeDisposable();
            // Apparently we get null ServerInfo (not even endpoints :S)
            var obs = ServerResponses
                .TakeUntil(RefreshComplete)
                .Select(x => x.ServerInfo == null ? null : ArmaServerInfo.FromWrap(x.ServerIndex, x.ServerInfo))
                .Replay();
            dsp.Add(obs.Connect());
            obs.Subscribe(_ => { }, x => dsp.Dispose(), dsp.Dispose, ct);
            ct.Register(dsp.Dispose);
            await GetServerInfo(filter).ConfigureAwait(false);
            return obs;
        }

        public async Task<IObservable<ArmaServerInfo>> GetServersInclDetails(CancellationToken ct,
            ServerFilterWrap filter, bool inclRules) {
            var obs = PrepareListener(ct, inclRules);
            await GetServerInfoInclDetails(filter).ConfigureAwait(false);
            return obs;
        }

        private IConnectableObservable<ArmaServerInfo> PrepareListener(CancellationToken ct, bool inclRules = false) {
            var dsp = new CompositeDisposable();
            var obs = BuildListener(dsp);
            if (inclRules)
                obs = obs
                    .SelectMany(async si => {
                        await UpdateRules(si, ct).ConfigureAwait(false);
                        return si;
                    });
            var theObs = obs.Select(x => x.Info)
                .Replay();
            dsp.Add(theObs.Connect());
            theObs.Subscribe(_ => { }, x => dsp.Dispose(), dsp.Dispose, ct);
            ct.Register(dsp.Dispose);
            return theObs;
        }

        private IObservable<ArmaServer> BuildListener(CompositeDisposable dsp) => ServerResponses
            .TakeUntil(RefreshComplete)
            .Where(x => x.ServerInfo != null)
            .SelectMany(ToServerInfo)
            .Do(dsp.Add);

        private async Task<ArmaServer> ToServerInfo(ServerRespondedEventArgs x) {
            var info = ArmaServerInfo.FromWrap(x.ServerIndex, x.ServerInfo);
            return new ArmaServer(info, await _fetcherFact(info.QueryEndPoint).ConfigureAwait(false));
        }

        async Task UpdateRules(ArmaServer si, CancellationToken ct) {
            try {
                await si.UpdateRules(ct).ConfigureAwait(false);
            } catch (Exception ex) {
                si.LastRuleError = ex;
            }
        }
    }

    public class ServerInfoFetcher : IDisposable
    {
        private readonly LockedWrapper<MatchmakingServiceWrap> _api;

        public ServerInfoFetcher(LockedWrapper<MatchmakingServiceWrap> a) {
            _api = a;
            ServerResponses =
                Observable.FromEvent<EventHandler<ServerRespondedEventArgs>, ServerRespondedEventArgs>(handler => {
                        EventHandler<ServerRespondedEventArgs> evtHandler = (sender, e) => handler(e);
                        return evtHandler;
                    },
                    evtHandler => _api.DoWithoutLock(api => api.ServerResponded += evtHandler),
                    evtHandler => _api.DoWithoutLock(api => api.ServerResponded -= evtHandler),
                    _api.Scheduler);

            RefreshComplete =
                Observable.FromEvent<EventHandler<RefreshCompletedEventArgs>, RefreshCompletedEventArgs>(handler => {
                        EventHandler<RefreshCompletedEventArgs> evtHandler = (sender, e) => handler(e);
                        return evtHandler;
                    },
                    evtHandler => _api.DoWithoutLock(api => api.RefreshComplete += evtHandler),
                    evtHandler => _api.DoWithoutLock(api => api.RefreshComplete -= evtHandler),
                    _api.Scheduler);

            ServerFailedToRespond =
                Observable.FromEvent<EventHandler<ServerFailedToRespondEventArgs>, ServerFailedToRespondEventArgs>(
                    handler => {
                        EventHandler<ServerFailedToRespondEventArgs> evtHandler = (sender, e) => handler(e);
                        return evtHandler;
                    },
                    evtHandler => _api.DoWithoutLock(api => api.ServerFailedToRespond += evtHandler),
                    evtHandler => _api.DoWithoutLock(api => api.ServerFailedToRespond -= evtHandler),
                    _api.Scheduler);
        }

        public IObservable<ServerRespondedEventArgs> ServerResponses { get; }
        public IObservable<ServerFailedToRespondEventArgs> ServerFailedToRespond { get; }
        public IObservable<RefreshCompletedEventArgs> RefreshComplete { get; }

        public void Dispose() {
            Dispose(true);
        }

        protected Task GetServerInfo(ServerFilterWrap filter) => _api.Do(api => api.RequestInternetServerList(filter));

        protected Task GetServerInfoInclDetails(ServerFilterWrap filter)
            => _api.Do(api => api.RequestInternetServerListWithDetails(filter));

        public async Task<ArmaServerInfo> GetServerInfoInclDetails1(ServerFilterWrap filter, CancellationToken ct) {
            var obs = ServerResponses
                .Take(1)
                .TakeUntil(RefreshComplete.Void()
                    .Merge(ServerFailedToRespond.Void())
                    .Merge(Observable.Timer(TimeSpan.FromSeconds(5))
                        .Void())).FirstAsync().ToTask(ct);
            await GetServerInfoInclDetails(filter).ConfigureAwait(false);
            var r = await obs;
            return ArmaServerInfo.FromWrap(r.ServerIndex, r.ServerInfo);
        }

        protected virtual void Dispose(bool disposing) {
            _api.Do(api => api.CancelRequest()); // hmm
            _api.DoWithoutLock(api => api.Dispose());
        }
    }

    public class ServerInfoRulesFetcher : ServerInfoFetcher
    {
        private readonly IPEndPoint _ep;
        private readonly LockedWrapper<ServerRulesServiceWrap> _srs;

        public ServerInfoRulesFetcher(IPEndPoint ep, LockedWrapper<ServerRulesServiceWrap> s,
            LockedWrapper<MatchmakingServiceWrap> a) : base(a) {
            _ep = ep;
            _srs = s;
            RulesRefreshComplete =
                Observable
                    .FromEvent<EventHandler<ServerRulesRefreshCompletedEventArgs>, ServerRulesRefreshCompletedEventArgs>
                    (handler => {
                            EventHandler<ServerRulesRefreshCompletedEventArgs> evtHandler =
                                (sender, e) => handler(e);
                            return evtHandler;
                        },
                        evtHandler => _srs.DoWithoutLock(srs => srs.RefreshComplete += evtHandler),
                        evtHandler => _srs.DoWithoutLock(srs => srs.RefreshComplete -= evtHandler),
                        _srs.Scheduler);

            RulesFailedToRespond =
                Observable
                    .FromEvent
                    <EventHandler<ServerRulesFailedToRespondEventArgs>, ServerRulesFailedToRespondEventArgs>
                    (handler => {
                            EventHandler<ServerRulesFailedToRespondEventArgs> evtHandler = (sender, e) => handler(e);
                            return evtHandler;
                        },
                        evtHandler => _srs.DoWithoutLock(srs => srs.ServerRulesFailedToRespond += evtHandler),
                        evtHandler => _srs.DoWithoutLock(srs => srs.ServerRulesFailedToRespond -= evtHandler),
                        _srs.Scheduler);
        }

        public IObservable<ServerRulesFailedToRespondEventArgs> RulesFailedToRespond { get; set; }

        public IObservable<ServerRulesRefreshCompletedEventArgs> RulesRefreshComplete { get; set; }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _srs.Do(srs => srs.CancelRequest()); // hm
            _srs.DoWithoutLock(srs => srs.Dispose());
        }

        public async Task<ServerDataWrap> Fetch(CancellationToken ct) {
            var t = RulesRefreshComplete
                .Take(1)
                .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(3)).Void()
                    .Merge(RulesFailedToRespond.Void()).Take(1))
                .Select(x => x.ServerData)
                .SingleAsync()
                .ToTask(ct);
            await
                _srs.Do(
                        srs =>
                            srs.RequestServerRules(
                                BitConverter.ToInt32(_ep.Address.GetAddressBytes().Reverse().ToArray(), 0), _ep.Port))
                    .ConfigureAwait(false);
            try {
                return await t;
            } catch (Exception) {
                await _srs.Do(src => src.CancelRequest()).ConfigureAwait(false);
                throw;
            }
        }
    }

    public static class Exts
    {
        public static ServerFilterWrap GetServerFilterWrap(this ServerFilterBuilder This) {
            var f = new ServerFilterWrap();
            foreach (var d in This.Value) {
                f.AddFilter(d.Item1, d.Item2);
            }

            return f;
        }
    }

    class ArmaServer : IDisposable
    {
        private readonly ServerInfoRulesFetcher _fetcher;
        private readonly ServerFilterWrap _filter;

        public ArmaServer(ArmaServerInfo info, ServerInfoRulesFetcher fetcher) {
            Info = info;
            _fetcher = fetcher;
            _filter = ServerFilterBuilder.Build().FilterByAddress(info.ConnectionEndPoint).GetServerFilterWrap();
        }

        public ArmaServer(int index, ServerKey key, ServerInfoRulesFetcher fetcher)
            : this(new ArmaServerInfo(index, key), fetcher) {}

        public ArmaServerInfo Info { get; private set; }
        public Exception LastRuleError { get; set; }

        public void Dispose() {
            _fetcher.Dispose();
        }

        public async Task Update(CancellationToken ct) {
            await UpdateServerInfo(ct).ConfigureAwait(false);
            await UpdateRules(ct).ConfigureAwait(false);
        }

        private async Task UpdateServerInfo(CancellationToken ct)
            => Info = await _fetcher.GetServerInfoInclDetails1(_filter, ct).ConfigureAwait(false);

        public async Task UpdateRules(CancellationToken ct) {
            var d = await _fetcher.Fetch(ct).ConfigureAwait(false);
            Info.ApplyServerDataToServerInfo(d);
        }
    }

    public class ArmaServerInfo : ArmaServerInfoModel
    {
        static ArmaServerInfo() {
            var dictionary = new Dictionary<DLC, Dlcs> {
                {DLC.EKart, Dlcs.Karts},
                {DLC.EHeli, Dlcs.Helicopters},
                {DLC.EMarksmen, Dlcs.Marksmen},
                {DLC.EZeus, Dlcs.Zeus},
                {DLC.EExpansion, Dlcs.Apex},
                {DLC.ETanoa, Dlcs.Tanoa}
            };
            NativeToManagedDlcMap = dictionary;
        }

        public ArmaServerInfo(int index, ServerKey key) : base(key.ToIpEndpoint()) {
            Index = index;
            Key = key;
        }

        public ServerKey Key { get; private set; }
        public int Index { get; private set; }

        static Dictionary<DLC, Dlcs> NativeToManagedDlcMap { get; }

        public new List<ServerModInfo> ModList { get; set; }

        public static ArmaServerInfo FromWrap(int serverIndex, GameServerItemWrap serverData) {
            try {
                return new ArmaServerInfo(serverIndex, new ServerKey(serverData.IP, serverData.QueryPort)) {
                    ConnectionEndPoint = new ServerKey(serverData.IP, serverData.ConnectionPort).ToIpEndpoint(),
                    Name = serverData.Name,
                    Map = serverData.Map,
                    Mission = serverData.Description,
                    ServerVersion = serverData.ServerVersion,
                    RequirePassword = serverData.RequirePassword,
                    IsVacEnabled = serverData.IsVACSecure,
                    CurrentPlayers = serverData.Players,
                    MaxPlayers = serverData.MaxPlayers,
                    Ping = serverData.Ping,
                    Tags = serverData.Tags,
                    GameTags = GameTags.Parse(serverData.Tags)
                };
            } catch {
                return new ArmaServerInfo(serverIndex, new ServerKey(serverData.IP, serverData.QueryPort));
            }
        }

        public void ApplyServerDataToServerInfo(ServerDataWrap serverData) {
            ModList = serverData.Mods;
            foreach (var str in serverData.Signatures) {
                SignatureList.Add(str);
            }
            Difficulty = (Difficulty) serverData.Difficulty.Difficulty;
            AiLevel = (AiLevel) serverData.Difficulty.AiLevel;
            IsThirdPersonViewEnabled = serverData.Difficulty.IsThirdPersonCameraEnabled;
            HelicopterFlightModel = serverData.Difficulty.IsAdvancedFlightModelEnabled
                ? HelicopterFlightModel.Advanced
                : HelicopterFlightModel.Basic;
            IsWeaponCrosshairEnabled = serverData.Difficulty.IsWeaponCrosshairEnabled;
            IsModListOverflowed = serverData.IsModListOveflowed;
            IsSignatureListOverflowed = serverData.IsSignatureListOverflowed;
            RequiresExpansionTerrain = serverData.ExpansionTerrain;
            foreach (var pair in NativeToManagedDlcMap) {
                if (serverData.Dlc.HasFlag(pair.Key))
                    DownloadableContent |= pair.Value;
            }
            ReceivedRules = true;
        }
    }
}