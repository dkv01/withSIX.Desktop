// <copyright company="SIX Networks GmbH" file="ServerInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Api.Models.Servers;

namespace withSIX.Mini.Core.Games
{
    public class ServerInfo<T> : Server
    {
        public int Ping { get; set; }
        public T Details { get; set; }
    }

    public interface IQueryServers
    {
        Task<List<IPEndPoint>> GetServers(CancellationToken cancelToken);

        Task<List<Server>> GetServerInfos(IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails = false);
    }
}