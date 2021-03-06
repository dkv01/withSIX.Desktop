// <copyright company="SIX Networks GmbH" file="RemoveRecent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction]
    public class RemoveRecent : ICommand, IHaveId<Guid>, IHaveGameId
    {
        public RemoveRecent(Guid gameId, Guid id) {
            GameId = gameId;
            Id = id;
        }

        public Guid GameId { get; }

        public Guid Id { get; }
    }

    [ApiUserAction]
    public class ClearRecent : ICommand, IHaveId<Guid>
    {
        public ClearRecent(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }


    public class RemoveRecentHandler : DbCommandBase, IAsyncRequestHandler<RemoveRecent>,
        IAsyncRequestHandler<ClearRecent>
    {
        public RemoveRecentHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task Handle(ClearRecent request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            game.ClearRecent();
        }

        public async Task Handle(RemoveRecent request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var content = await game.Contents.FindOrThrowFromRequestAsync(request).ConfigureAwait(false);
            content.RemoveRecentInfo();
        }
    }
}