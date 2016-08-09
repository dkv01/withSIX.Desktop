// <copyright company="SIX Networks GmbH" file="GetServers.cs">
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
    public class GetServers : IAsyncQuery<ServersList>
    {
        public GetServers(GetServersQuery info) {
            Info = info;
        }

        public GetServersQuery Info { get; }
    }

    public class GetServersHandler : ApiDbQueryBase, IAsyncRequestHandler<GetServers, ServersList>
    {
        public GetServersHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<ServersList> Handle(GetServers request) {
            var game = await GameContext.FindGameOrThrowAsync(request.Info).ConfigureAwait(false);
            var sGame = game as IQueryServers;
            if (sGame == null)
                throw new ValidationException("Game does not support servers");
            return new ServersList {Addresses = await sGame.GetServers().ConfigureAwait(false)};
        }
    }

    public class GetServersQuery : IHaveGameId
    {
        public Guid GameId { get; set; }
    }

    public class ServersList
    {
        public List<IPEndPoint> Addresses { get; set; }
    }
}