// <copyright company="SIX Networks GmbH" file="IServerQueryParser.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace GameServerQuery
{
    public interface IServerQueryParser
    {
        ServerQueryResult ParsePackets(IResult result);
    }
}