// <copyright company="SIX Networks GmbH" file="ScanLocalContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features.Main.Games
{
    [Obsolete("No longer used")]
    public class ScanLocalContent : IAsyncVoidCommand, IHaveId<Guid>, IHaveGameId
    {
        public ScanLocalContent(Guid id) {
            Id = id;
        }

        public Guid GameId => Id;

        public Guid Id { get; }
    }

    public class ScanLocalContentHandler : DbCommandBase, IAsyncVoidCommandHandler<ScanLocalContent>
    {
        readonly ISetupGameStuff _setup;

        public ScanLocalContentHandler(IDbContextLocator dbContextLocator, ISetupGameStuff setup)
            : base(dbContextLocator) {
            _setup = setup;
        }

        public async Task<Unit> Handle(ScanLocalContent request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);

            await
                _setup.HandleGameContentsWhenNeeded(new ContentQuery {
                        PackageNames = game.LocalContent.OfType<ModLocalContent>().Select(x => x.PackageName).ToList()
                    }, game.Id)
                    .ConfigureAwait(false);

            return Unit.Value;
        }
    }
}