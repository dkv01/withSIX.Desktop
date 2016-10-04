// <copyright company="SIX Networks GmbH" file="IServerQueryParser.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Net;

namespace GameServerQuery
{
    public interface IServerQueryParser
    {
        ServerQueryResult ParsePackets(IPEndPoint address, IReadOnlyList<byte[]> receivedPackets, List<int> pings);
    }
}