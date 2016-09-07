// <copyright company="SIX Networks GmbH" file="RemoveRecent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction]
    public class RemoveRecent : IAsyncVoidCommand, IHaveId<Guid>, IHaveGameId
    {
        public RemoveRecent(Guid gameId, Guid id) {
            GameId = gameId;
            Id = id;
        }

        public Guid GameId { get; }

        public Guid Id { get; }
    }

    [ApiUserAction]
    public class ClearRecent : IAsyncVoidCommand, IHaveId<Guid>
    {
        public ClearRecent(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }


    public class RemoveRecentHandler : DbCommandBase, IAsyncVoidCommandHandler<RemoveRecent>,
        IAsyncVoidCommandHandler<ClearRecent>
    {
        public RemoveRecentHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<Unit> Handle(ClearRecent request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            game.ClearRecent();
            return Unit.Value;
        }

        public async Task<Unit> Handle(RemoveRecent request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var content = await game.Contents.FindOrThrowFromRequestAsync(request).ConfigureAwait(false);
            content.RemoveRecentInfo();
            return Unit.Value;
        }
    }
}