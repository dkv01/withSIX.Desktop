// <copyright company="SIX Networks GmbH" file="RemoteEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Core.Applications
{
    public interface IEvent {}

    public class RemoteEvent
    {
        public RemoteEvent(string type) {
            Type = type;
        }

        public string Type { get; }
    }

    public class RemoteEventData : RemoteEvent
    {
        public RemoteEventData(string type) : base(type) { }
        public string Data { get; set; }
    }

    public class EventsModel
    {
        public List<RemoteEventData> Events { get; set; }
    }

    public class Drainer : IDisposable
    {
        private readonly CancellationTokenSource _cts;

        public Drainer() {
            _cts = new CancellationTokenSource();
        }

        public void Dispose() {
            _cts.Cancel();
            _cts.Dispose();
        }

        public async Task Drain() {
            while (!_cts.IsCancellationRequested) {
                var r = await new Uri("http://127.0.0.66:48667/api/get-events")
                    .GetJson<EventsModel>().ConfigureAwait(false);
                foreach (var e in r.Events) {
                    var type = Type.GetType(e.Type);
                    var evt = JsonConvert.DeserializeObject(e.Data, type, JsonSupport.DefaultSettings);
                }
            }
        }
    }
}