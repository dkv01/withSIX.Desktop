// <copyright company="SIX Networks GmbH" file="IServerQueryParser.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Play.Core.Games.Legacy.ServerQuery
{
    public interface IServerQueryParser
    {
        ServerQueryResult ParsePackets(ServerQueryState state);
    }
}