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
        protected ServerQueryResult(IPEndPoint address, IDictionary<string, string> settings, bool isMasterResult = false) {
            Contract.Requires<ArgumentNullException>(address != null);
            Contract.Requires<ArgumentNullException>(settings != null);
            Address = address;
            Settings = settings;
            IsMasterResult = isMasterResult;
        }

        public bool IsMasterResult { get; private set; }
        public int Ping { get; set; } = ServerQueryState.MagicPingValue;
        public IDictionary<string, string> Settings { get; }
        public List<Player> Players { get; set; }
        public abstract ServerQueryMode Mode { get; }
        public IPEndPoint Address { get; }

        public string GetSettingOrDefault(string settingName)
            => Settings.ContainsKey(settingName) ? Settings[settingName] : null;
    }

    public class GamespyServerQueryResult : ServerQueryResult
    {
        public GamespyServerQueryResult(IPEndPoint ep, IDictionary<string, string> settings, bool isMasterResult = false)
            : base(ep, settings, isMasterResult) {}

        public override ServerQueryMode Mode
        {
            get { return ServerQueryMode.Gamespy; }
        }
    }

    public class SourceServerQueryResult : ServerQueryResult
    {
        public SourceServerQueryResult(IPEndPoint ep, IDictionary<string, string> settings, bool isMasterResult = false)
            : base(ep, settings, isMasterResult) {}

        public override ServerQueryMode Mode
        {
            get { return ServerQueryMode.Steam; }
        }
    }

    public class SourceMasterServerQueryResult : ServerQueryResult
    {
        public SourceMasterServerQueryResult(IPEndPoint ep, IDictionary<string, string> settings) : base(ep, settings, true) {}
        public override ServerQueryMode Mode
        {
            get { return ServerQueryMode.Steam; }
        }
    }
}