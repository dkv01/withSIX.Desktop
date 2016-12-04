// <copyright company="SIX Networks GmbH" file="CloseGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction("Close")]
    public class CloseGame : RequestBase, IHaveId<Guid>, IAsyncVoidCommand
    {
        public CloseGame(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class CloseGameHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<CloseGame>
    {
        public CloseGameHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<Unit> Handle(CloseGame request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            game.Close();
            return Unit.Value;
        }
    }
}