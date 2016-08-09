// <copyright company="SIX Networks GmbH" file="GetServersInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Applications.Usecases.Main.Servers
{
    public class GetServersInfo : IAsyncQuery<ServersInfo>
    {
        public GetServersInfo(GetServerQuery info) {
            Info = info;
        }

        public GetServerQuery Info { get; }
    }

    public class GetServersInfoHandler : ApiDbQueryBase, IAsyncRequestHandler<GetServersInfo, ServersInfo>
    {
        public GetServersInfoHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<ServersInfo> Handle(GetServersInfo request) {
            var game = await GameContext.FindGameOrThrowAsync(request.Info).ConfigureAwait(false);
            var sGame = game as IQueryServers;
            if (sGame == null)
                throw new ValidationException("Game does not support servers");
            return new ServersInfo {
                Servers =
                    await
                        sGame.GetServerInfos(request.Info.Addresses, request.Info.IncludePlayers).ConfigureAwait(false)
            };
        }
    }

    public class GetServerQuery : IHaveGameId
    {
        public List<IPEndPoint> Addresses { get; set; }
        public bool IncludePlayers { get; set; }
        public Guid GameId { get; set; }
    }
}