// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;

namespace withSIX.Steam.Core.Requests
{
    public class GetServers
    {
        public Guid GameId { get; set; }
        public List<Tuple<string, string>> Filter { get; set; }
        public bool IncludeRules { get; set; }
        public bool IncludeDetails { get; set; }
        public int PageSize { get; set; } = 24;
    }

    public class GetServerInfo
    {
        public Guid GameId { get; set; }
        public List<IPEndPoint> Addresses { get; set; }
        public bool IncludeRules { get; set; }
        public bool IncludeDetails { get; set; }
    }
}