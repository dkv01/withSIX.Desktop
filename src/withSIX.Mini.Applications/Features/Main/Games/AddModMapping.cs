using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Attributes;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features.Main.Games
{
    public class AddModMapping : ICommand, IHaveId<Guid>, IHaveGameId
    {
        public AddModMapping(Guid id, Guid gameId, string packageName) {
            Id = id;
            GameId = gameId;
            PackageName = packageName;
        }

        [ValidUuid]
        public Guid Id { get; }
        [ValidUuid]
        public Guid GameId { get; }
        [Required, StringLength(255, MinimumLength = 1)]
        public string PackageName { get; }
    }

    public class AddModMappingHandler : DbCommandBase, IAsyncRequestHandler<AddModMapping>
    {
        public AddModMappingHandler(IDbContextLocator dbContextLocator)
            : base(dbContextLocator) {}

        public async Task Handle(AddModMapping request)
        {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            game.Mappings[request.PackageName] = request.Id;
        }
    }
}