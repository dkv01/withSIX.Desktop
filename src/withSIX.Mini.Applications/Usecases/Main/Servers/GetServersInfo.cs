// <copyright company="SIX Networks GmbH" file="GetServersInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Servers;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Applications.Usecases.Main.Servers
{
    public class GetServersInfo : IAsyncQuery<ServersInfo>, IRequireConnectionId, IRequireRequestId
    {
        public GetServersInfo(GetServerQuery info) {
            Info = info;
        }

        public GetServerQuery Info { get; }
        public string ConnectionId { get; set; }
        public Guid RequestId { get; set; }
    }

    public class GetServersInfoHandler : ApiDbQueryBase, IAsyncRequestHandler<GetServersInfo, ServersInfo>
    {
        private readonly IServerQueryFactory _sqf;
        private readonly IMessageBusProxy _mb;

        public GetServersInfoHandler(IDbContextLocator dbContextLocator, IServerQueryFactory sqf, IMessageBusProxy mb) : base(dbContextLocator) {
            _sqf = sqf;
            _mb = mb;
        }

        public async Task<ServersInfo> Handle(GetServersInfo request) {
            var game = await GameContext.FindGameOrThrowAsync(request.Info).ConfigureAwait(false);
            var sGame = game as IQueryServers;
            if (sGame == null)
                throw new ValidationException("Game does not support servers");
            var servers = new List<Server>();
            await
                sGame.GetServerInfos(_sqf, request.Info.Addresses,
                        x => {
                            _mb.SendMessage(new ServerInfoReceived(game.Id, new List<Server> {x}).ToClientEvent(request));
                            servers.Add(x);
                        },
                        request.Info.IncludePlayers)
                    .ConfigureAwait(false);
            return new ServersInfo {Servers = servers};
        }
    }

    public class ServerInfoReceived
    {
        public Guid GameId { get; }
        public List<Server> Items { get; }

        public ServerInfoReceived(Guid gameId, List<Server> items) {
            GameId = gameId;
            Items = items;
        }
    }

    public class GetServerQuery : IHaveGameId
    {
        public List<IPEndPoint> Addresses { get; set; }
        public bool IncludePlayers { get; set; }
        public Guid GameId { get; set; }
    }
}