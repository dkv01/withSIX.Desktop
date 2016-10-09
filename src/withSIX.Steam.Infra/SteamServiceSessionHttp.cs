// <copyright company="SIX Networks GmbH" file="SteamServiceSessionHttp.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    // TODO: The eventdrainer need to be finished and enabled on both sides..
    public class SteamServiceSessionHttp : SteamServiceSession
    {
        private Uri _uri;

        public override Task Start(uint appId, Uri uri) {
            _uri = uri;
            var t2 = TaskExt.StartLongRunningTask(async () => {
                using (var drainer = new Drainer(uri)) {
                    await drainer.Drain().ConfigureAwait(false);
                }
            });
            // TODO: Test the connection? 
            return TaskExt.Default;
        }

        public override async Task<BatchResult> GetServerInfo<T>(GetServerInfo query, Action<List<T>> pageAction, CancellationToken ct) {
            var r = await query.PostJson<ServersInfo<T>>(new Uri(_uri, "/api/get-server-info"), ct).ConfigureAwait(false);
            pageAction(r.Servers);
            return new BatchResult(r.Servers.Count);
        }

        public override async Task<BatchResult> GetServers<T>(GetServers query, Action<List<T>> pageAction, CancellationToken ct) {
            throw new NotImplementedException();
            var r = await query.PostJson<BatchResult>(new Uri(_uri, "/api/get-servers"), ct).ConfigureAwait(false);
            return r;
        }

        public override async Task<BatchResult> GetServerAddresses(GetServerAddresses query,
            Action<List<IPEndPoint>> pageAction, CancellationToken ct) {
            throw new NotImplementedException();
            var r =
                await query.PostJson<BatchResult>(new Uri(_uri, "/api/get-server-addresses"), ct).ConfigureAwait(false);
            return r;
        }
    }

    public static class TempExtensions
    {
        public static async Task<T> PostJson<T>(this object model, Uri uri,
            CancellationToken ct = default(CancellationToken)) {
            var r = await model.PostJson(uri, ct).ConfigureAwait(false);
            return r.FromJson<T>();
        }
    }


    public class Drainer : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly Uri _uri;

        public Drainer(Uri uri) {
            _uri = uri;
            _cts = new CancellationTokenSource();
        }

        public void Dispose() {
            _cts.Cancel();
            _cts.Dispose();
        }

        public async Task Drain() {
            while (!_cts.IsCancellationRequested) {
                var r = await new Uri(_uri, "/api/get-events")
                    .GetJson<EventsModel>().ConfigureAwait(false);
                foreach (var e in r.Events) {
                    var type = Type.GetType(e.Type);
                    var evt = JsonConvert.DeserializeObject(e.Data, type, JsonSupport.DefaultSettings);
                    //TODO
                }
            }
        }
    }
}