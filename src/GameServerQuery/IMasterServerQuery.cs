// <copyright company="SIX Networks GmbH" file="IMasterServerQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GameServerQuery
{
    public interface IMasterServerQuery
    {
        Task<List<IPEndPoint>> GetParsedServers(CancellationToken cancelToken, int limit,
            IPEndPoint remote = null, int tried = -1);
    }
}