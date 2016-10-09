// <copyright company="SIX Networks GmbH" file="SteamServiceSessionSignalR.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Steam.Core.Requests;

namespace withSIX.Steam.Infra
{
    public class SteamServiceSessionSignalR : SteamServiceSession
    {
        private static readonly JsonSerializer jsonSerializer = JsonSerializer.Create(JsonSupport.DefaultSettings);
        private readonly DefaultHttpClient _defaultHttpClient = new DefaultHttpClient();
        private readonly AsyncLock _l = new AsyncLock();
        private readonly ISubject<IEvent> _subject;
        private HubConnection _con;
        private IDisposable _dsp;
        private IHubProxy _servers;

        public SteamServiceSessionSignalR() {
            _subject = Subject.Synchronize(new Subject<IEvent>());
        }

        public override Task Start(uint appId, Uri uri) {
            _con?.Stop();
            _dsp?.Dispose();
            CompositeDisposable dsp;
            _dsp = dsp = new CompositeDisposable();
            HubConnection con;
            dsp.Add(con = _con = new HubConnection(new Uri(uri, "/signalr").ToString()) {
                JsonSerializer = jsonSerializer
            });
            _servers = con.CreateHubProxy("ServerHub");
            dsp.Add(_servers.On<ReceivedServerAddressesPageEvent>("ServerAddressesPageReceived", RaiseEvent));
            dsp.Add(_servers.On<ReceivedServerPageEvent>("ServerPageReceived", RaiseEvent));
            dsp.Add(_servers.On<ReceivedServerEvent>("ServerReceived", RaiseEvent));
            return Connect();
        }

        private void RaiseEvent<T>(T x) where T : IEvent => _subject.OnNext(x);

        private Task Connect() => _con.Start(_defaultHttpClient);

        public override async Task<BatchResult> GetServerInfo<T>(GetServerInfo query, Action<List<T>> pageAction,
            CancellationToken ct) {
            await MakeSureConnected().ConfigureAwait(false);
            var requestId = Guid.NewGuid();
            using (SetupListener(pageAction)) {
                var r = await _servers.Invoke<BatchResult>("GetServerInfo", query, requestId).ConfigureAwait(false);
                return r;
            }
        }

        public override async Task<BatchResult> GetServers<T>(GetServers query, Action<List<T>> pageAction,
            CancellationToken ct) {
            var requestId = Guid.NewGuid();
            using (SetupListener(pageAction)) {
                var r = await _servers.Invoke<BatchResult>("GetServers", query, requestId).ConfigureAwait(false);
                return r;
            }
        }

        public override async Task<BatchResult> GetServerAddresses(GetServerAddresses query,
            Action<List<IPEndPoint>> pageAction, CancellationToken ct) {
            var requestId = Guid.NewGuid();
            using (_subject.OfType<ReceivedServerAddressesPageEvent>()
                //.Where(x => x.GameId = request.GameId || requestId) 
                .Select(x => x.Servers)
                .Do(pageAction)
                .Subscribe()) {
                var r = await _servers.Invoke<BatchResult>("GetServerAddresses", query, requestId).ConfigureAwait(false);
                return r;
            }
        }

        private IDisposable SetupListener<T>(Action<List<T>> pageAction) =>
            _subject.OfType<ReceivedServerPageEvent>()
                //.Where(x => x.GameId = request.GameId || requestId) 
                // TODO: Skip json step
                .Select(x => x.Servers.Select(s => (T) JsonSupport.FromJson<T>(JsonSupport.ToJson(s))).ToList())
                .Do(pageAction)
                .Subscribe();

        private async Task MakeSureConnected() {
            using (await _l.LockAsync().ConfigureAwait(false)) {
                if (_con.State == ConnectionState.Disconnected)
                    await Connect().ConfigureAwait(false);
            }
        }
    }

    public class ReceivedServerPageEvent : IEvent
    {
        public Guid GameId { get; set; }
        public List<dynamic> Servers { get; set; }
    }

    public class ReceivedServerAddressesPageEvent : IEvent
    {
        public Guid GameId { get; set; }
        public List<IPEndPoint> Servers { get; set; }
    }

    public class ReceivedServerEvent : IEvent
    {
        public Guid GameId { get; set; }
        public dynamic ServerInfo { get; set; }
    }
}