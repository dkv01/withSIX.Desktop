// <copyright company="SIX Networks GmbH" file="ISupportServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Core.Games.Entities
{
    public interface ISupportServers : IQueryServers
    {
        IFilter GetServerFilter();

        [Obsolete]
        bool SupportsServerType(string type);

        Server CreateServer(ServerAddress address);
    }
}