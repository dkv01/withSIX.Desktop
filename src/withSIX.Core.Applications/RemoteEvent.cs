// <copyright company="SIX Networks GmbH" file="RemoteEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;

namespace withSIX.Core.Applications
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
        public RemoteEventData(string type) : base(type) {}
        public string Data { get; set; }
    }

    public class EventsModel
    {
        public List<RemoteEventData> Events { get; set; }
    }
}