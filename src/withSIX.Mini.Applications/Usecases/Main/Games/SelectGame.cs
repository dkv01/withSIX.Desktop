// <copyright company="SIX Networks GmbH" file="SelectGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Usecases.Main.Games
{
    public class SelectGame : IAsyncVoidCommand, IHaveId<Guid>, IHaveGameId, IExcludeGameWriteLock
    {
        public SelectGame(Guid id) {
            Id = id;
        }

        public Guid GameId => Id;

        public Guid Id { get; }
    }

    public class SelectGameHandler : DbCommandBase, IAsyncVoidCommandHandler<SelectGame>
    {
        readonly IGameSwitcher _gameSwitcher;

        public SelectGameHandler(IDbContextLocator dbContextLocator, IGameSwitcher gameSwitcher)
            : base(dbContextLocator) {
            _gameSwitcher = gameSwitcher;
        }

        public async Task<Unit> Handle(SelectGame request) {
            await _gameSwitcher.SwitchGame(request.Id).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}