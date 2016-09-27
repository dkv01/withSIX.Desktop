// <copyright company="SIX Networks GmbH" file="IMasterServerQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameServerQuery
{
    public interface IMasterServerQuery
    {
        Task<IEnumerable<ServerQueryResult>> GetParsedServers(CancellationToken cancelToken, bool forceLocal = false,
            int limit = 0);
    }
}