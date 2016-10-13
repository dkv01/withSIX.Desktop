// <copyright company="SIX Networks GmbH" file="ServerInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Api.Models.Servers;
using withSIX.Core;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Core.Games
{
    public interface IQueryServers
    {
        Task<BatchResult> GetServers(IServerQueryFactory factory, CancellationToken cancelToken, Action<List<IPEndPoint>> act);

        Task<BatchResult> GetServerInfos(IServerQueryFactory factory, IReadOnlyCollection<IPEndPoint> addresses,
            Action<Server> act, bool inclExtendedDetails = false);
    }
}