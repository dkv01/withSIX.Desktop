// <copyright company="SIX Networks GmbH" file="ServerQueryResult.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using GameServerQuery.Parsers;

namespace GameServerQuery
{
    public abstract class ServerQueryResult
    {
        protected ServerQueryResult(IPEndPoint address, ParseResult settings, bool isMasterResult = false) {
            Contract.Requires<ArgumentNullException>(address != null);
            Contract.Requires<ArgumentNullException>(settings != null);
            Address = address;
            Settings = settings;
            IsMasterResult = isMasterResult;
        }

        public bool IsMasterResult { get; private set; }
        public int Ping { get; set; } = 9999;
        public ParseResult Settings { get; }
        public List<Player> Players { get; set; }
        public abstract ServerQueryMode Mode { get; }
        public IPEndPoint Address { get; }
    }

    public class GamespyServerQueryResult : ServerQueryResult
    {
        public GamespyServerQueryResult(IPEndPoint ep, ParseResult settings, bool isMasterResult = false)
            : base(ep, settings, isMasterResult) {}

        public override ServerQueryMode Mode => ServerQueryMode.Gamespy;
    }

    public class SourceServerQueryResult : ServerQueryResult
    {
        public SourceServerQueryResult(IPEndPoint ep, SourceParseResult settings, bool isMasterResult = false)
            : base(ep, settings, isMasterResult) {}

        public override ServerQueryMode Mode => ServerQueryMode.Steam;
    }
}