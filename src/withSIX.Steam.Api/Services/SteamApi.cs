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
                var r = RefreshComplete.ToTask();
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

        class ServerResponder
        {
            private readonly int _serverId;
            private readonly uint ip;
            private readonly ushort port;

            private ISteamMatchmakingPingResponse m_PingResponse;
            private readonly ISteamMatchmakingPlayersResponse m_PlayersResponse;
            private readonly ISteamMatchmakingRulesResponse m_RulesResponse;


            public ServerResponder(IPEndPoint ep) {
                // todo ep to ip/port;

                m_PingResponse = new ISteamMatchmakingPingResponse(OnServerResponded, OnServerFailedToRespond);
                m_PlayersResponse = new ISteamMatchmakingPlayersResponse(OnAddPlayerToList, OnPlayersFailedToRespond,
                    OnPlayersRefreshComplete);
                m_RulesResponse = new ISteamMatchmakingRulesResponse(OnRulesResponded, OnRulesFailedToRespond,
                    OnRulesRefreshComplete);
            }

            // TODO: Update ServerDetails; Use ServerBrowser, filter by specific IP, then get details

            public void GetRules() {
                var q = SteamMatchmakingServers.ServerRules(ip, port, m_RulesResponse);
            }

            public void GetPlayers() {
                var q = SteamMatchmakingServers.PlayerDetails(ip, port, m_PlayersResponse);
            }

            private void OnRulesRefreshComplete() {
                throw new NotImplementedException();
            }

            private void OnPlayersRefreshComplete() {
                throw new NotImplementedException();
            }

            private void OnRulesFailedToRespond() {
                throw new NotImplementedException();
            }

            private void OnPlayersFailedToRespond() {
                throw new NotImplementedException();
            }

            private void OnRulesResponded(string pchrule, string pchvalue) {
                throw new NotImplementedException();
            }

            private void OnAddPlayerToList(string pchname, int nscore, float fltimeplayed) {
                throw new NotImplementedException();
            }

            private void OnServerFailedToRespond() {
                throw new NotImplementedException();
            }

            private void OnServerResponded(gameserveritem_t server) {}
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