using System;
using System.Collections.Generic;
using System.Net;
using SN.withSIX.Core;

namespace SN.withSIX.Mini.Core.Games
{
    public class ServersPageReceived : IAsyncDomainEvent
    {
        public Guid GameId { get; }
        public List<IPEndPoint> Items { get; }

        public ServersPageReceived(Guid gameId, List<IPEndPoint> items) {
            GameId = gameId;
            Items = items;
        }
    }
}