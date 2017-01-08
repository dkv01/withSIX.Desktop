// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Applications.Features.Main.Servers
{
    public class GetServerAddresses : IQuery<BatchResult>
    {
        public GetServerAddresses(GetServersQuery info) {
            Info = info;
        }

        public GetServersQuery Info { get; }
    }

    public class GetServerAddressesHandler : ApiDbQueryBase,
        ICancellableAsyncRequestHandler<GetServerAddresses, BatchResult>
    {
        private readonly IRequestScopeLocator _scopeLoc;
        private readonly IServerQueryFactory _sqf;

        public GetServerAddressesHandler(IDbContextLocator dbContextLocator, IServerQueryFactory sqf,
            IRequestScopeLocator scopeLoc) : base(dbContextLocator) {
            _sqf = sqf;
            _scopeLoc = scopeLoc;
        }

        public async Task<BatchResult> Handle(GetServerAddresses request, CancellationToken cancelToken) {
            var game = await GameContext.FindGameOrThrowAsync(request.Info).ConfigureAwait(false);
            var sGame = game as IQueryServers;
            if (sGame == null)
                throw new ValidationException("Game does not support servers");
            var fetched = false;
            var scope = _scopeLoc.Scope;
            var addresses = await
                DbContextLocator.GetApiContext()
                    .GetOrAddServers(game.Id, async () => {
                        fetched = true;
                        var list = new List<IPEndPoint>();
                        await sGame.GetServers(_sqf, cancelToken, x => {
                            list.AddRange(x);
                            scope.SendMessage(new ServersPageReceived(game.Id, x));
                        }).ConfigureAwait(false);
                        return list;
                    })
                    .ConfigureAwait(false);
            if (!fetched) {
                foreach (var b in addresses.Batch(231)) {
                    cancelToken.ThrowIfCancellationRequested();
                    scope.SendMessage(new ServersPageReceived(game.Id, b.ToList()));
                }
            }
            return new BatchResult(addresses.Count);
        }
    }

    public class GetServersQuery : IHaveGameId
    {
        public Guid GameId { get; set; }
    }
}