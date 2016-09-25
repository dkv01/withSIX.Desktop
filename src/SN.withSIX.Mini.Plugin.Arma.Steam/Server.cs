// <copyright company="SIX Networks GmbH" file="Server.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NDepend.Helpers;
using NDepend.Path;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Mini.Plugin.Arma.Models;
using SteamLayerWrap;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;
using ServerModInfo = SteamLayerWrap.ServerModInfo;

namespace SN.withSIX.Mini.Plugin.Arma.Steam
{
    public interface ISafeCall<out T>
    {
        void Do(Action<T> act);
        TResult Do<TResult>(Func<T, TResult> action);
    }

    public interface ISafeCallFactory
    {
        ISafeCall<T> Create<T>(T val);
    }

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
        private readonly ISteamApi _steamApi;
        private LockedWrapper<MatchmakingServiceWrap> _api;

        public ServerBrowser(ISteamApi steamApi) : base(steamApi) {
            _steamApi = steamApi;
            _api = steamApi.CreateMatchmakingServiceWrap();
        }

        public async Task<ServerDataWrap> GetServerRules(IPEndPoint ep,
            CancellationToken ct = default(CancellationToken)) {
            using (var srs = new ServerInfoRulesFetcher(ep, _steamApi)) {
                return await srs.Fetch(ct).ConfigureAwait(false);
            }
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _api.Do(api => api.CancelRequest());
            _api.DoWithoutLock(api => api.Dispose());
            _api = null;
        }

        public IObservable<ArmaServerInfo> GetServers(CancellationToken ct, ServerFilterWrap filter) {
            var dsp = new CompositeDisposable();
            // Apparently we get null ServerInfo (not even endpoints :S)
            var obs = ServerResponses
                .Select(x => x.ServerInfo == null ? null : ArmaServerInfo.FromWrap(x.ServerIndex, x.ServerInfo))
                .TakeUntil(RefreshComplete)
                .Replay();
            dsp.Add(obs.Connect());
            obs.Subscribe(_ => { }, x => dsp.Dispose(), dsp.Dispose, ct);
            ct.Register(dsp.Dispose);
            dsp.Add(GetServerInfo(filter, ct));
            return obs;
        }

        public IObservable<ArmaServerInfo> GetServersInclDetails(CancellationToken ct, ServerFilterWrap filter, bool inclRules) {
            var dsp = new CompositeDisposable();
            var obs = PrepareListener(ct, dsp, inclRules);
            dsp.Add(GetServerInfoInclDetails(filter, ct));
            return obs;
        }

        private IConnectableObservable<ArmaServerInfo> PrepareListener(CancellationToken ct, CompositeDisposable dsp,
            bool inclRules = false) {
            var obs = BuildListener(dsp);
            if (inclRules)
                obs = obs.MergeTask<ArmaServer, ArmaServer>(async si => {
                    await UpdateRules(si, ct).ConfigureAwait(false);
                    return si;
                }, 1); // 10 fails, and specifying nothing too!
            var theObs = obs.Select(x => x.Info)
                .Replay();
            dsp.Add(theObs.Connect());
            theObs.Subscribe(_ => { }, x => dsp.Dispose(), dsp.Dispose, ct);
            ct.Register(dsp.Dispose);
            return theObs;
        }

        private IObservable<ArmaServer> BuildListener(CompositeDisposable dsp) => ServerResponses
            .Where(x => x.ServerInfo != null)
            .Select(ToServerInfo)
            .Do(dsp.Add)
            .TakeUntil(RefreshComplete);

        private ArmaServer ToServerInfo(ServerRespondedEventArgs x) {
            var info = ArmaServerInfo.FromWrap(x.ServerIndex, x.ServerInfo);
            return new ArmaServer(info, _steamApi);
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

        public ServerInfoFetcher(ISteamApi steamApi) {
            _api = steamApi.CreateMatchmakingServiceWrap();

            ServerResponses =
                Observable.FromEvent<EventHandler<ServerRespondedEventArgs>, ServerRespondedEventArgs>(handler => {
                        EventHandler<ServerRespondedEventArgs> evtHandler = (sender, e) => handler(e);
                        return evtHandler;
                    },
                    evtHandler => _api.DoWithoutLock(api => api.ServerResponded += evtHandler),
                    evtHandler => _api.DoWithoutLock(api => api.ServerResponded -= evtHandler));

            RefreshComplete =
                Observable.FromEvent<EventHandler<RefreshCompletedEventArgs>, RefreshCompletedEventArgs>(handler => {
                        EventHandler<RefreshCompletedEventArgs> evtHandler = (sender, e) => handler(e);
                        return evtHandler;
                    },
                    evtHandler => _api.DoWithoutLock(api => api.RefreshComplete += evtHandler),
                    evtHandler => _api.DoWithoutLock(api => api.RefreshComplete -= evtHandler));

            ServerFailedToRespond =
                Observable.FromEvent<EventHandler<ServerFailedToRespondEventArgs>, ServerFailedToRespondEventArgs>(
                    handler => {
                        EventHandler<ServerFailedToRespondEventArgs> evtHandler = (sender, e) => handler(e);
                        return evtHandler;
                    },
                    evtHandler => _api.DoWithoutLock(api => api.ServerFailedToRespond += evtHandler),
                    evtHandler => _api.DoWithoutLock(api => api.ServerFailedToRespond -= evtHandler));
        }

        public IObservable<ServerRespondedEventArgs> ServerResponses { get; }
        public IObservable<ServerFailedToRespondEventArgs> ServerFailedToRespond { get; }
        public IObservable<RefreshCompletedEventArgs> RefreshComplete { get; }

        public void Dispose() {
            Dispose(true);
        }

        public IDisposable GetServerInfo(ServerFilterWrap filter, CancellationToken ct) {
            _api.Do(api => api.RequestInternetServerList(filter));
            return ct.Register(() => _api.Do(api => api.CancelRequest()));
        }

        public IDisposable GetServerInfoInclDetails(ServerFilterWrap filter, CancellationToken ct) {
            _api.DoWithoutLock(api => api.RequestInternetServerListWithDetails(filter));
            return ct.Register(() => _api.Do(api => api.CancelRequest()));
        }

        public async Task<ArmaServerInfo> GetServerInfoInclDetails1(ServerFilterWrap filter, CancellationToken ct) {
            var obs = ServerResponses.Take(1)
                .TakeUntil(RefreshComplete.Select(x => Unit.Default)
                    .Merge(ServerFailedToRespond.Select(x => Unit.Default))
                    .Merge(ServerResponses.Throttle(TimeSpan.FromSeconds(3))
                        .Select(x => Unit.Default))).FirstAsync().ToTask(ct);
            using (GetServerInfoInclDetails(filter, ct)) {
                var r = await obs;
                return ArmaServerInfo.FromWrap(r.ServerIndex, r.ServerInfo);
            }
        }

        protected virtual void Dispose(bool disposing) {
            _api.Do(api => api.CancelRequest());
            _api.DoWithoutLock(api => api.Dispose());
            _api = null;
        }
    }

    public class ServerInfoRulesFetcher : ServerInfoFetcher
    {
        private readonly IPEndPoint _ep;
        private LockedWrapper<ServerRulesServiceWrap> _srs;

        public ServerInfoRulesFetcher(IPEndPoint ep, ISteamApi api) : base(api) {
            _ep = ep;
            _srs = api.CreateRulesManagerWrap();
            RulesRefreshComplete =
                Observable
                    .FromEvent
                    <EventHandler<ServerRulesRefreshCompletedEventArgs>, ServerRulesRefreshCompletedEventArgs>
                    (handler => {
                            EventHandler<ServerRulesRefreshCompletedEventArgs> evtHandler =
                                (sender, e) => handler(e);
                            return evtHandler;
                        },
                        evtHandler => _srs.DoWithoutLock(srs => srs.RefreshComplete += evtHandler),
                        evtHandler => _srs.DoWithoutLock(srs => srs.RefreshComplete -= evtHandler));

            RulesFailedToRespond =
                Observable
                    .FromEvent
                    <EventHandler<ServerRulesFailedToRespondEventArgs>, ServerRulesFailedToRespondEventArgs>
                    (handler => {
                            EventHandler<ServerRulesFailedToRespondEventArgs> evtHandler = (sender, e) => handler(e);
                            return evtHandler;
                        },
                        evtHandler => _srs.DoWithoutLock(srs => srs.ServerRulesFailedToRespond += evtHandler),
                        evtHandler => _srs.DoWithoutLock(srs => srs.ServerRulesFailedToRespond -= evtHandler));
        }

        public IObservable<ServerRulesFailedToRespondEventArgs> RulesFailedToRespond { get; set; }

        public IObservable<ServerRulesRefreshCompletedEventArgs> RulesRefreshComplete { get; set; }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _srs.Do(srs => srs.CancelRequest());
            _srs.DoWithoutLock(srs => srs.Dispose());
            _srs = null;
        }

        public async Task<ServerDataWrap> Fetch(CancellationToken ct) {
            var t = RulesRefreshComplete
                .Take(1)
                .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(3)).Select(x => Unit.Default)
                    .Merge(RulesFailedToRespond.Select(x => Unit.Default)).Take(1))
                .Select(x => x.ServerData)
                .SingleAsync()
                .ToTask(ct);
            _srs.Do(srs => srs.RequestServerRules(BitConverter.ToInt32(_ep.Address.GetAddressBytes().Reverse().ToArray(), 0),
                _ep.Port));
            try {
                using (ct.Register(() => _srs.Do(srs => srs.CancelRequest())))
                    return await t;
            } catch (Exception) {
                _srs.Do(src => src.CancelRequest());
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
        LockedWrapper<MatchmakingServiceWrap> CreateMatchmakingServiceWrap();
        LockedWrapper<ServerRulesServiceWrap> CreateRulesManagerWrap();
    }

    // TODO: Use the scheduler approach
    public class SteamApi : ISteamApi
    {
        private readonly LockedWrapper<ISteamAPIWrap> _steamApi;

        public SteamApi(ISteamAPIWrap apiWrap) {
            _steamApi = new LockedWrapper<ISteamAPIWrap>(apiWrap);
        }

        public uint AppId { get; private set; }

        public LockedWrapper<MatchmakingServiceWrap> CreateMatchmakingServiceWrap()
            => _steamApi.Do(t => new LockedWrapper<MatchmakingServiceWrap>(t.CreateMatchmakingService(), _steamApi.SyncRoot));

        public LockedWrapper<ServerRulesServiceWrap> CreateRulesManagerWrap() => _steamApi.Do(t => new LockedWrapper<ServerRulesServiceWrap>(t.CreateServerRulesService()));

        public async Task Initialize(IAbsoluteDirectoryPath gamePath) {
            var appId = (uint) SteamGameIds.Arma3;
            if (AppId == appId)
                return;
            await SetupAppId(appId).ConfigureAwait(false);
            _steamApi.Do(x => {
                var managerConfigWrap = new ManagerConfigWrap {ConsumerAppId = appId};
                managerConfigWrap.Load(gamePath.GetChildFileWithName(@"Launcher\config.bin").ToString());
                return x.Init(managerConfigWrap);
            });
            var runner = new SteamAPIRunner(_steamApi);
            runner.Run();
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

    public class LockedWrapper<T> where T : class
    {
        private readonly T _obj;
        private readonly ISafeCall<T> _safeCall;

        public LockedWrapper(T obj) : this(obj, new object()) {}

        public LockedWrapper(T obj, object syncRoot) {
            if (obj == null) {
                throw new ArgumentNullException("obj");
            }
            SyncRoot = syncRoot;
            _safeCall = SteamAPIRunner.callFactory.Create(obj);
            _obj = obj;
        }

        public object SyncRoot { get; }

        public void Do(Action<T> action) {
            lock (SyncRoot) {
                _safeCall.Do(action);
            }
        }

        public TResult Do<TResult>(Func<T, TResult> action) {
            lock (SyncRoot) {
                return _safeCall.Do(action);
            }
        }

        public void DoWithoutLock(Action<T> action) => _safeCall.Do(action);

        public TResult DoWithoutLock<TResult>(Func<T, TResult> action) => _safeCall.Do(action);
    }


    class ArmaServer : IDisposable
    {
        private readonly ServerInfoRulesFetcher _fetcher;
        private readonly ServerFilterWrap _filter;

        public ArmaServer(ArmaServerInfo info, ISteamApi steamApi) {
            Info = info;
            _fetcher = new ServerInfoRulesFetcher(info.QueryEndPoint, steamApi);
            _filter = ServerFilterBuilder.Build().FilterByAddress(info.ConnectionEndPoint).Value;
        }

        public ArmaServer(int index, ServerKey key, ISteamApi steamApi) : this(new ArmaServerInfo(index, key), steamApi) {}
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
   
    public class SteamAPIRunner
    {
        private static readonly TimeSpan UpdateIntervalDefault = TimeSpan.FromMilliseconds(50.0);
        private static readonly TimeSpan UpdateIntervalSlow = TimeSpan.FromMilliseconds(1000.0);
        private readonly LockedWrapper<ISteamAPIWrap> _api;
        private TimeSpan _updateInterval = UpdateIntervalDefault;

        public SteamAPIRunner([NotNull] LockedWrapper<ISteamAPIWrap> steamApi) {
            if (steamApi == null) {
                throw new ArgumentNullException("steamApi");
            }
            _api = steamApi;
        }

        public bool IsRunning { get; private set; }

        public static ISafeCallFactory callFactory { get; set; }

        public event EventHandler AfterTick;

        public event EventHandler BeforeTick;

        public void Run() {
            if (!IsRunning) {
                IsRunning = true;
                var thread2 = new Thread(Start) {
                    IsBackground = true
                    //Priority = ThreadPriority.BelowNormal
                };
                thread2.Start();
            }
        }

        public void SetNormalSpeed() {
            _updateInterval = UpdateIntervalDefault;
        }

        public void SetSlowSpeed() {
            _updateInterval = UpdateIntervalSlow;
        }

        private void Start() {
            while (IsRunning) {
                //this.BeforeTick.Raise(this, EventArgs.Empty);
                if (!IsRunning) {
                    return;
                }
                try {
                    _api.Do(api => {
                        var safeCall = callFactory.Create(api);
                        safeCall.Do(a => a.Simulate());
                    });
                } catch (Exception ex) {
                    Console.WriteLine("Error during SteamApiRunner " + ex);
                } catch {
                    Console.WriteLine("Native error during SteamApiRunner");
                }

                if (!IsRunning) {
                    return;
                }
                //this.AfterTick.Raise(this, EventArgs.Empty);
                if (!IsRunning) {
                    return;
                }
                Thread.Sleep(_updateInterval);
            }
        }

        public void Stop() {
            if (IsRunning) {
                IsRunning = false;
            }
        }
    }
}