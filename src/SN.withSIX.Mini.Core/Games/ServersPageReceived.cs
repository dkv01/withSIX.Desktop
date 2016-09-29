// <copyright company="SIX Networks GmbH" file="ServersPageReceived.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using withSIX.Core;

namespace withSIX.Mini.Core.Games
{
    public class ServersPageReceived : IAsyncDomainEvent
    {
        public ServersPageReceived(Guid gameId, List<IPEndPoint> items) {
            GameId = gameId;
            Items = items;
        }

        public Guid GameId { get; }
        public List<IPEndPoint> Items { get; }
    }
}