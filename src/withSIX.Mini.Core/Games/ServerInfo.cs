// <copyright company="SIX Networks GmbH" file="ServerInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Api.Models.Servers;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Core.Games
{
    public class ServerInfo<T> : Server
    {
        public int Ping { get; set; }
        public T Details { get; set; }
    }

    public interface IQueryServers
    {
        Task<List<IPEndPoint>> GetServers(IServerQueryFactory factory, CancellationToken cancelToken);

        Task<List<Server>> GetServerInfos(IServerQueryFactory factory, IReadOnlyCollection<IPEndPoint> addresses, bool inclExtendedDetails = false);
    }
}