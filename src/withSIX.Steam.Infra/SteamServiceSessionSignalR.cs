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
using Newtonsoft.Json.Linq;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    public class SteamServiceSessionSignalR : SteamServiceSession
    {
        private readonly AsyncLock _l = new AsyncLock();
        private HubConnection _con;
        private readonly DefaultHttpClient _defaultHttpClient = new DefaultHttpClient();
        private IHubProxy _servers;
        private IDisposable _dsp;
        private readonly ISubject<IEvent> _subject;

        public SteamServiceSessionSignalR() {
            _subject = Subject.Synchronize(new Subject<IEvent>());
        }

        public override Task Start(uint appId, Uri uri) {
            _con?.Stop();
            _dsp?.Dispose();
            var con = _con = new HubConnection(new Uri(uri, "/signalr").ToString()) {
                JsonSerializer = JsonSerializer.Create(JsonSupport.DefaultSettings)
            };
            CompositeDisposable dsp;
            _dsp = dsp = new CompositeDisposable();
            _servers = con.CreateHubProxy("ServerHub");
            dsp.Add(con);
            var spr = _servers.On<ReceivedServerPageEvent>("ServerPageReceived", _subject.OnNext);
            dsp.Add(spr);
            var rs = _servers.On<ReceivedServerEvent>("ServerReceived", _subject.OnNext);
            dsp.Add(rs);
            return Connect();
        }

        private Task Connect() => _con.Start(_defaultHttpClient);

        public override async Task<BatchResult> GetServerInfo<T>(GetServerInfo query, Action<List<T>> pageAction, CancellationToken ct) {
            await MakeSureConnected().ConfigureAwait(false);
            var requestId = Guid.NewGuid();
            using (SetupListener(pageAction)) {
                var r = await _servers.Invoke<BatchResult>("GetServerInfo", query, requestId).ConfigureAwait(false);
                return r;
            }
        }

        public override async Task<BatchResult> GetServers<T>(GetServers query, Action<List<T>> pageAction, CancellationToken ct) {
            var requestId = Guid.NewGuid();
            using (SetupListener(pageAction)) {
                var r = await _servers.Invoke<BatchResult>("GetServers", query, requestId).ConfigureAwait(false);
                return r;
            }
        }

        private IDisposable SetupListener<T>(Action<List<T>> pageAction) {
            return _subject.OfType<ReceivedServerPageEvent>()
                //.Where(x => x.GameId = request.GameId || requestId) 
                .Subscribe(x => pageAction(x.Servers.Select(s => ((object) s).MapTo<T>()).ToList()));
        }

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

    public class ReceivedServerEvent : IEvent
    {
        public Guid GameId { get; set; }
        public dynamic ServerInfo { get; set; }
    }
}