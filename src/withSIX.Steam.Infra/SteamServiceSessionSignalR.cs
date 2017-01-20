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
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    public class SteamServiceSessionSignalR : SteamServiceSession, IDisposable
    {
        static readonly JsonSerializer jsonSerializer = JsonSerializer.Create(ApiSerializerSettings.GetSettings());
        private readonly DefaultHttpClient _defaultHttpClient = new DefaultHttpClient();
        private readonly AsyncLock _l = new AsyncLock();
        private readonly ISubject<object> _subject;
        private HubConnection _con;
        private IDisposable _dsp;
        private IHubProxy _servers;

        public SteamServiceSessionSignalR() {
            _subject = Subject.Synchronize(new Subject<object>());
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
            dsp.Add(_servers.On<ReceivedServerAddressesPageEvent, Guid>("ServerAddressesPageReceived", RaiseEvent));
            dsp.Add(_servers.On<ReceivedServerPageEvent, Guid>("ServerPageReceived", RaiseEvent));
            return Connect();
        }

        private void RaiseEvent<T>(T x, Guid requestId) where T : IEvent => _subject.OnNext(Tuple.Create(x, requestId));

        private Task Connect() => _con.Start(_defaultHttpClient);

        public override async Task<BatchResult> GetServers<T>(GetServers query, Action<List<T>> pageAction,
            CancellationToken ct) {
            var requestId = Guid.NewGuid();
            await MakeSureConnected().ConfigureAwait(false);
            using (Listen<ReceivedServerPageEvent>(requestId)
                .Select(x => x.Servers.Cast<T>().ToList())
                .Do(pageAction)
                .Subscribe()) {
                return
                    await _servers.Invoke<BatchResult>("GetServers", query, requestId)
                        .MakeCancellable(ct)
                        .ConfigureAwait(false);
            }
        }

        public override async Task<BatchResult> GetServerAddresses(GetServerAddresses query,
            Action<List<IPEndPoint>> pageAction, CancellationToken ct) {
            var requestId = Guid.NewGuid();

            await MakeSureConnected().ConfigureAwait(false);
            using (Listen<ReceivedServerAddressesPageEvent>(requestId)
                .Select(x => x.Servers)
                .Do(pageAction)
                .Subscribe()) {
                return
                    await _servers.Invoke<BatchResult>("GetServerAddresses", query, requestId)
                        .MakeCancellable(ct)
                        .ConfigureAwait(false);
            }
        }

        private IObservable<T> Listen<T>(Guid requestId) => _subject.OfType<Tuple<T, Guid>>()
            .Where(x => x.Item2 == requestId)
            .Select(x => x.Item1);

        private async Task MakeSureConnected() {
            using (await _l.LockAsync().ConfigureAwait(false)) {
                if (_con.State == ConnectionState.Disconnected)
                    await Connect().ConfigureAwait(false);
            }
        }

        public void Dispose() {
            _con?.Stop();
            _dsp?.Dispose();
        }
    }

    public abstract class ReceivedPageEvent<T> : IEvent
    {
        protected ReceivedPageEvent(List<T> servers) {
            Servers = servers;
        }

        public List<T> Servers { get; set; }
    }

    public class ReceivedServerPageEvent : ReceivedPageEvent<ServerInfoModel>
    {
        public ReceivedServerPageEvent(List<ServerInfoModel> servers) : base(servers) {}
    }

    public class ReceivedServerAddressesPageEvent : ReceivedPageEvent<IPEndPoint>
    {
        public ReceivedServerAddressesPageEvent(List<IPEndPoint> servers) : base(servers) {}
    }
}