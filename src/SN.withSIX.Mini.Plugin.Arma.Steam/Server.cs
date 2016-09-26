﻿// <copyright company="SIX Networks GmbH" file="Server.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NDepend.Path;
using SN.withSIX.Mini.Plugin.Arma.Models;
using SteamLayerWrap;
using withSIX.Api.Models.Extensions;
using ServerModInfo = SteamLayerWrap.ServerModInfo;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;

namespace withSIX.Steam.Plugin.Arma
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ServerKey
    {
        public uint IpAddress { get; }
        public ushort Port { get; }

        public ServerKey(uint ipAddress, ushort port) {
            this = new ServerKey();
            IpAddress = ipAddress;
            Port = port;
        }

        public bool Equals(ServerKey other) => (IpAddress == other.IpAddress) && (Port == other.Port);

        public override bool Equals([CanBeNull] object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is ServerKey && Equals((ServerKey) obj);
        }

        public override int GetHashCode() => (int) (IpAddress*0x18d) ^ Port.GetHashCode();

        public override string ToString() => string.Format("({0} {2}:{1})", IpAddress, Port, ToIpAddress());

        public IPAddress ToIpAddress() => new IPAddress(ReverseBytes(IpAddress));
        public IPEndPoint ToIpEndpoint() => new IPEndPoint(ToIpAddress(), Port);

        private static uint ReverseBytes(uint value) => (uint)
        (((value & 0xff) << 0x18) | ((value & 0xff00) << 8) | ((value & 0xff0000) >> 8) |
         ((value & -16777216) >> 0x18));
    }

    public class ServerBrowser : ServerInfoFetcher
    {
        private readonly Func<IPEndPoint, Task<ServerInfoRulesFetcher>> _fetcherFact;

        public ServerBrowser(LockedWrapper<MatchmakingServiceWrap> api, Func<IPEndPoint, Task<ServerInfoRulesFetcher>> fetcherFact) : base(api) {
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

        public async Task<IObservable<ArmaServerInfo>> GetServersInclDetails(CancellationToken ct, ServerFilterWrap filter, bool inclRules) {
            var dsp = new CompositeDisposable();
            var obs = PrepareListener(ct, dsp, inclRules);
            await GetServerInfoInclDetails(filter).ConfigureAwait(false);
            return obs;
        }

        private IConnectableObservable<ArmaServerInfo> PrepareListener(CancellationToken ct, CompositeDisposable dsp,
            bool inclRules = false) {
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
        private LockedWrapper<MatchmakingServiceWrap> _api;

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

        protected Task GetServerInfoInclDetails(ServerFilterWrap filter) => _api.Do(api => api.RequestInternetServerListWithDetails(filter));

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
            _api = null;
        }
    }

    public class ServerInfoRulesFetcher : ServerInfoFetcher
    {
        private readonly IPEndPoint _ep;
        private LockedWrapper<ServerRulesServiceWrap> _srs;

        public ServerInfoRulesFetcher(IPEndPoint ep, LockedWrapper<ServerRulesServiceWrap> s, LockedWrapper<MatchmakingServiceWrap> a) : base(a) {
            _ep = ep;
            _srs = s;
            RulesRefreshComplete =
                Observable.FromEvent<EventHandler<ServerRulesRefreshCompletedEventArgs>, ServerRulesRefreshCompletedEventArgs>
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
            _srs = null;
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

    public class ServerFilterBuilder
    {
        private readonly ServerFilterWrap _filter;
        private bool _isFinal;

        public ServerFilterBuilder() {
            _filter = new ServerFilterWrap();
        }

        public ServerFilterWrap Value
        {
            get
            {
                _isFinal = true;
                return _filter;
            }
        }

        public static ServerFilterBuilder Build() => new ServerFilterBuilder();

        public ServerFilterBuilder FilterByAddresses(System.Collections.Generic.IReadOnlyCollection<IPEndPoint> list) {
            MakeFilterStep();
            AddOr(list, x => FilterByAddress(x));
            return this;
        }

        void AddOr<T>(System.Collections.Generic.IReadOnlyCollection<T> list, Action<T> act) {
            _filter.AddFilter("or", list.Count.ToString());
            foreach (var point in list)
                act(point);
        }

        public ServerFilterBuilder FilterByAddress(IPEndPoint point) {
            _filter.AddFilter("gameaddr", $"{point.Address}:{point.Port}");
            return this;
        }

        private void MakeFilterStep() {
            if (_isFinal)
                throw new NotSupportedException("Already obtained the value");
        }
    }

    public interface ISteamApi
    {
        Task Initialize(IAbsoluteDirectoryPath gamePath, uint appId);
        Task<LockedWrapper<MatchmakingServiceWrap>> CreateMatchmakingServiceWrap();
        Task<LockedWrapper<ServerRulesServiceWrap>> CreateRulesManagerWrap();
    }

    // TODO: Use the scheduler approach
    public class SteamApi : ISteamApi
    {
        private readonly IScheduler _scheduler;
        private readonly LockedWrapper<ISteamAPIWrap> _steamApi;

        public SteamApi(ISteamAPIWrap apiWrap, IScheduler scheduler) {
            _scheduler = scheduler;
            _steamApi = new LockedWrapper<ISteamAPIWrap>(apiWrap, scheduler);
        }

        public uint AppId { get; private set; }

        public Task<LockedWrapper<MatchmakingServiceWrap>> CreateMatchmakingServiceWrap()
            => _steamApi.Do(t => new LockedWrapper<MatchmakingServiceWrap>(t.CreateMatchmakingService(), _scheduler));

        public Task<LockedWrapper<ServerRulesServiceWrap>> CreateRulesManagerWrap()
            => _steamApi.Do(t => new LockedWrapper<ServerRulesServiceWrap>(t.CreateServerRulesService(), _scheduler));

        public async Task Initialize(IAbsoluteDirectoryPath gamePath, uint appId) {
            /*
            if (AppId == appId)
                return;
            await SetupAppId(appId).ConfigureAwait(false);
            */
            var r = await _steamApi.Do(x => {
                var managerConfigWrap = new ManagerConfigWrap {ConsumerAppId = appId};
                managerConfigWrap.Load(gamePath.GetChildFileWithName(@"Launcher\config.bin").ToString());
                return x.Init(managerConfigWrap);
            }).ConfigureAwait(false);
            if (r == InitResult.SteamNotRunning)
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            if (r == InitResult.APIInitFailed)
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            if (r == InitResult.ContextCreationFailed)
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            if (r == InitResult.AlreadyInitialized)
                throw new SteamInitializationException(
                    "Steam initialization failed. Already initialized");
            if (r == InitResult.Disabled)
                throw new SteamInitializationException(
                    "Steam initialization failed. Disabled");
        }

        private async Task SetupAppId(uint appId) {
            if (AppId > 0)
                throw new InvalidOperationException("This session is already initialized!");
            AppId = appId;
            var tmp =
                Directory.GetCurrentDirectory().ToAbsoluteDirectoryPath().GetChildFileWithName("steam_appid.txt");
            await WriteSteamAppId(appId, tmp).ConfigureAwait(false);
        }

        private static Task WriteSteamAppId(uint appId, IAbsoluteFilePath steamAppIdFile)
            => appId.ToString().WriteToFileAsync(steamAppIdFile);
    }

    public abstract class LockedWrapper {
        public static ISafeCallFactory callFactory { get; set; }
    }

    public class LockedWrapper<T> : LockedWrapper where T : class
    {
        private readonly T _obj;
        private readonly IScheduler _scheduler;
        private readonly ISafeCall _safeCall;

        public IScheduler Scheduler => _scheduler;

        public LockedWrapper(T obj, IScheduler scheduler) {
            if (obj == null) {
                throw new ArgumentNullException("obj");
            }
            _safeCall = callFactory.Create();
            _obj = obj;
            _scheduler = scheduler;
        }


        public async Task Do(Action<T> action) => await Observable.Return(Unit.Default, _scheduler)
            .Do(_ => _safeCall.Do(() => action(_obj)));

        public async Task<TResult> Do<TResult>(Func<T, TResult> action)
            => await Observable.Return(Unit.Default, _scheduler)
                .Select(_ => _safeCall.Do(() => action(_obj)));

        public void DoWithoutLock(Action<T> action) => _safeCall.Do(() => action(_obj));

        public TResult DoWithoutLock<TResult>(Func<T, TResult> action) => _safeCall.Do(() => action(_obj));
    }

    class ArmaServer : IDisposable
    {
        private readonly ServerInfoRulesFetcher _fetcher;
        private readonly ServerFilterWrap _filter;

        public ArmaServer(ArmaServerInfo info, ServerInfoRulesFetcher fetcher) {
            Info = info;
            _fetcher = fetcher;
            _filter = ServerFilterBuilder.Build().FilterByAddress(info.ConnectionEndPoint).Value;
        }

        public ArmaServer(int index, ServerKey key, ServerInfoRulesFetcher fetcher) : this(new ArmaServerInfo(index, key), fetcher) {}
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
            var dictionary = new Dictionary<DLC, Dlcs>();
            dictionary.Add(DLC.EKart, Dlcs.Karts);
            dictionary.Add(DLC.EHeli, Dlcs.Helicopters);
            dictionary.Add(DLC.EMarksmen, Dlcs.Marksmen);
            dictionary.Add(DLC.EZeus, Dlcs.Zeus);
            dictionary.Add(DLC.EExpansion, Dlcs.Apex);
            dictionary.Add(DLC.ETanoa, Dlcs.Tanoa);
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