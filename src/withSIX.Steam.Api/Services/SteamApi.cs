// <copyright company="SIX Networks GmbH" file="SteamApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using withSIX.Core.Services;
using withSIX.Steam.Api.Helpers;

namespace withSIX.Steam.Api.Services
{
    public class SteamApi : ISteamApi, IDomainService
    {
        private readonly ISteamSessionLocator _sessionLocator;

        public SteamApi(ISteamSessionLocator sessionLocator) {
            _sessionLocator = sessionLocator;
        }

        public async Task SubscribeAndConfirm(PublishedFileId_t pid) {
            var r = await SubscribeToContent(pid);
            ConfirmResult(r.m_eResult);
        }

        public async Task UnsubscribeAndConfirm(PublishedFileId_t pid) {
            var r = await UnsubscribeFromContent(pid);
            ConfirmResult(r.m_eResult);
        }

        public void ConfirmResult(EResult mEResult) {
            if (mEResult != EResult.k_EResultOK)
                throw new FailedOperationException("Failed with result " + mEResult, mEResult);
        }

        public IScheduler Scheduler => _sessionLocator.Session.Scheduler;

        public ItemInstallInfo GetItemInstallInfo(PublishedFileId_t pid) {
            ulong sizeOnDisk;
            string folder;
            uint folderSize = 0; // ?
            uint timestamp;
            return SteamUGC.GetItemInstallInfo(pid, out sizeOnDisk, out folder, folderSize, out timestamp)
                ? new ItemInstallInfo(folder, sizeOnDisk, folderSize, timestamp)
                : null;
            //throw new InvalidOperationException("Item is not actually considered installed?");
        }

        public IObservable<T> CreateObservableFromCallback<T>(CancellationToken cancelToken)
            => Observable.Create<T>(observer => {
                var callback = Callback<T>.Create(observer.OnNext);
                var r = cancelToken.Register(observer.HandleCanceled);
                return () => {
                    callback.Unregister();
                    r.Dispose();
                };
            }).ObserveOn(_sessionLocator.Session.Scheduler);

        public async Task<int> GetServerInfo(uint appId, Action<gameserveritem_t> act,
            List<Tuple<string, string>> filter) {
            using (var sb = new ServerBrowser()) {
                using (sb.ServerInfoReceived.Subscribe(act))
                    return await sb.GetServerList(appId, filter).ConfigureAwait(false);
            }
        }

        public async Task<IDictionary<string, string>> GetServerRules(IPEndPoint ep) {
            using (var sb = new ServerResponder(ep))
                return await sb.GetRules().ConfigureAwait(false);
        }

        private IObservable<RemoteStorageSubscribePublishedFileResult_t> SubscribeToContent(PublishedFileId_t pid)
            => ProcessCallback<RemoteStorageSubscribePublishedFileResult_t>(SteamUGC.SubscribeItem(pid));

        private IObservable<RemoteStorageUnsubscribePublishedFileResult_t> UnsubscribeFromContent(
                PublishedFileId_t pid)
            => ProcessCallback<RemoteStorageUnsubscribePublishedFileResult_t>(SteamUGC.UnsubscribeItem(pid));

        private IObservable<T> ProcessCallback<T>(SteamAPICall_t call) =>
            call.CreateObservableFromCallresults<T>(_sessionLocator.Session)
                .Take(1);

        public IObservable<T> CreateObservableFromCallback<T>()
            => Observable.Create<T>(observer => {
                var callback = Callback<T>.Create(observer.OnNext);
                return callback.Unregister;
            }).ObserveOn(_sessionLocator.Session.Scheduler);

        class ServerBrowser : IDisposable
        {
            private readonly Subject<Unit> _refreshComplete = new Subject<Unit>();
            private readonly Subject<gameserveritem_t> _serverInfoReceived = new Subject<gameserveritem_t>();
            private readonly ISteamMatchmakingServerListResponse m_ServerListResponse;
            private HServerListRequest _request;
            private int _resultsReceived;

            public ServerBrowser() {
                m_ServerListResponse = new ISteamMatchmakingServerListResponse(OnServerResponded,
                    OnServerFailedToRespond, OnRefreshComplete);
            }

            public IObservable<Unit> RefreshComplete => _refreshComplete.AsObservable();

            public IObservable<gameserveritem_t> ServerInfoReceived => _serverInfoReceived.AsObservable();

            public void Dispose() {
                if (_request != default(HServerListRequest))
                    SteamMatchmakingServers.CancelQuery(_request);
                _refreshComplete.Dispose();
                _serverInfoReceived.Dispose();
            }

            public async Task<int> GetServerList(uint appId, List<Tuple<string, string>> filter) {
                var r = RefreshComplete.Take(1).ToTask();
                _request = SteamMatchmakingServers.RequestInternetServerList(new AppId_t(appId),
                    filter.Select(x => new MatchMakingKeyValuePair_t {m_szKey = x.Item1, m_szValue = x.Item2}).ToArray(),
                    (uint) filter.Count, m_ServerListResponse);
                await r;
                return _resultsReceived;
            }

            private void OnRefreshComplete(HServerListRequest hrequest, EMatchMakingServerResponse response) {
                _refreshComplete.OnNext(Unit.Default);
            }

            private void OnServerFailedToRespond(HServerListRequest hrequest, int iserver) {}

            private void OnServerResponded(HServerListRequest hrequest, int iserver) {
                var s = SteamMatchmakingServers.GetServerDetails(hrequest, iserver);
                _resultsReceived++;
                _serverInfoReceived.OnNext(s);
            }
        }

        class ServerResponder : IDisposable
        {
            private readonly Subject<Unit> _pingFailed = new Subject<Unit>();
            private readonly uint _ip;

            private readonly ISteamMatchmakingPingResponse _mPingResponse;
            private readonly ISteamMatchmakingPlayersResponse _mPlayersResponse;
            private readonly ISteamMatchmakingRulesResponse _mRulesResponse;
            private readonly Subject<Unit> _playerCompleted = new Subject<Unit>();
            private readonly Subject<Unit> _playerFailed = new Subject<Unit>();
            private readonly Subject<Tuple<string, int, float>> _playerResponded =
                new Subject<Tuple<string, int, float>>();
            private readonly ushort _port;
            private readonly Subject<gameserveritem_t> _pingResponded = new Subject<gameserveritem_t>();
            private readonly Subject<Unit> _rulesCompleted = new Subject<Unit>();
            private readonly Subject<Unit> _rulesFailed = new Subject<Unit>();
            private readonly Subject<Tuple<string, string>> _rulesResponded = new Subject<Tuple<string, string>>();
            private HServerQuery _request;

            public ServerResponder(IPEndPoint ep) {
                _ip = (uint) BitConverter.ToInt32(ep.Address.GetAddressBytes().Reverse().ToArray(), 0);
                _port = (ushort) ep.Port;

                _mPingResponse = new ISteamMatchmakingPingResponse(OnServerResponded, OnServerFailedToRespond);
                _mPlayersResponse = new ISteamMatchmakingPlayersResponse(OnAddPlayerToList, OnPlayersFailedToRespond,
                    OnPlayersRefreshComplete);
                _mRulesResponse = new ISteamMatchmakingRulesResponse(OnRulesResponded, OnRulesFailedToRespond,
                    OnRulesRefreshComplete);
            }

            public void Dispose() {
                _rulesCompleted.Dispose();
                _rulesFailed.Dispose();
                _rulesResponded.Dispose();

                _pingFailed.Dispose();
                _pingResponded.Dispose();

                _playerResponded.Dispose();
                _playerCompleted.Dispose();
                _playerFailed.Dispose();
            }

            public async Task<gameserveritem_t> GetDetails() {
                var complete = _pingFailed.Take(1).Merge(_pingResponded.Take(1).Select(x => Unit.Default)).ToTask();
                var r = _pingResponded.TakeUntil(_pingFailed);
                _request = SteamMatchmakingServers.PingServer(_ip, _port, _mPingResponse);
                await complete;
                return await r;
            }

            public async Task<IDictionary<string, string>> GetRules() {
                var completeObs = _rulesCompleted.Take(1).Merge(_rulesFailed.Take(1));
                //var complete = completeObs.ToTask();
                var dict = _rulesResponded.TakeUntil(completeObs).ToDictionary(x => x.Item1, x => x.Item2);
                _request = SteamMatchmakingServers.ServerRules(_ip, _port, _mRulesResponse);
                //await complete;
                return await dict;
            }

            public async Task<IList<Tuple<string, int, float>>> GetPlayers() {
                var completeObs = _playerCompleted.Take(1).Merge(_playerFailed.Take(1));
                var complete = completeObs.ToTask();
                var dict = _playerResponded.TakeUntil(completeObs).ToList();
                _request = SteamMatchmakingServers.PlayerDetails(_ip, _port, _mPlayersResponse);
                await complete;
                return await dict;
            }

            private void OnRulesRefreshComplete() => _rulesCompleted.OnNext(Unit.Default);

            private void OnPlayersRefreshComplete() => _playerCompleted.OnNext(Unit.Default);

            private void OnRulesFailedToRespond() => _rulesFailed.OnNext(Unit.Default);

            private void OnRulesResponded(string pchrule, string pchvalue)
                => _rulesResponded.OnNext(Tuple.Create(pchrule, pchvalue));

            private void OnPlayersFailedToRespond() => _playerFailed.OnNext(Unit.Default);

            private void OnAddPlayerToList(string pchname, int nscore, float fltimeplayed)
                => _playerResponded.OnNext(Tuple.Create(pchname, nscore, fltimeplayed));

            private void OnServerFailedToRespond() => _pingFailed.OnNext(Unit.Default);

            private void OnServerResponded(gameserveritem_t server) => _pingResponded.OnNext(server);
        }
    }

    public class ItemInstallInfo
    {
        public ItemInstallInfo(string location, ulong sizeOnDisk, uint folderSize, uint timestamp) {
            Location = location;
            SizeOnDisk = sizeOnDisk;
            FolderSize = folderSize;
            Timestamp = timestamp;
        }

        public string Location { get; }
        public ulong SizeOnDisk { get; }
        public uint FolderSize { get; }
        public uint Timestamp { get; }
    }

    class FailedOperationException : InvalidOperationException
    {
        public FailedOperationException(string message, EResult result) : base(message) {
            Result = result;
        }

        public EResult Result { get; }
    }

    public interface ISteamApi
    {
        IScheduler Scheduler { get; }
        Task SubscribeAndConfirm(PublishedFileId_t pid);
        Task UnsubscribeAndConfirm(PublishedFileId_t pid);
        void ConfirmResult(EResult mEResult);
        IObservable<T> CreateObservableFromCallback<T>(CancellationToken cancelToken);
        ItemInstallInfo GetItemInstallInfo(PublishedFileId_t pid);
    }
}