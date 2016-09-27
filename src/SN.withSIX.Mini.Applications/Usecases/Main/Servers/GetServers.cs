// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Mini.Applications.Usecases.Main.Servers
{
    public class GetServers : IAsyncQuery<BatchResult>
    {
        public GetServers(GetServersQuery info) {
            Info = info;
        }

        public GetServersQuery Info { get; }
    }

    public class GetServersHandler : ApiDbQueryBase, IAsyncRequestHandler<GetServers, BatchResult>
    {
        public GetServersHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<BatchResult> Handle(GetServers request) {
            var game = await GameContext.FindGameOrThrowAsync(request.Info).ConfigureAwait(false);
            var sGame = game as IQueryServers;
            if (sGame == null)
                throw new ValidationException("Game does not support servers");
            var fetched = false;
            var addresses = await
                DbContextLocator.GetApiContext()
                    .GetOrAddServers(game.Id, async () => {
                        fetched = true;
                        return await sGame.GetServers().ConfigureAwait(false);
                    })
                    .ConfigureAwait(false);
            if (!fetched) {
                foreach (var b in addresses.Batch(231))
                    await new ServersPageReceived(game.Id, b.ToList()).Raise().ConfigureAwait(false);
            }
            return new BatchResult(addresses.Count);
        }
    }

    public class BatchResult
    {
        public BatchResult(int count) {
            Count = count;
        }

        public int Count { get; }
    }

    public class GetServersQuery : IHaveGameId
    {
        public Guid GameId { get; set; }
    }
}