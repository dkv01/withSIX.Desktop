// <copyright company="SIX Networks GmbH" file="QueryServersInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Usecases.Main.Servers
{
    public class QueryServersInfo : IAsyncQuery<ServersInfo>
    {
        public QueryServersInfo(GetServersInfoQuery info) {
            Info = info;
        }

        public GetServersInfoQuery Info { get; }
    }

    public class QueryServersInfoHandler : ApiDbQueryBase, IAsyncRequestHandler<QueryServersInfo, ServersInfo>
    {
        public QueryServersInfoHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public Task<ServersInfo> Handle(QueryServersInfo request) {
            throw new NotImplementedException();
        }
    }

    public class GetServersInfoQuery
    {
        public Guid GameId { get; set; }
        public string Name { get; set; }
        public string MissionName { get; set; }
        public string MapName { get; set; }
        public int? MinPlayers { get; set; }
        public int? MaxPlayers { get; set; }
    }

    public class ServersInfo
    {
        public List<ServerInfo> Servers { get; set; }
    }
}