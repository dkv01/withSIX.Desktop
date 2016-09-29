// <copyright company="SIX Networks GmbH" file="GameSwitcher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core.Applications.Services;
using withSIX.Core.Helpers;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games.Services;

namespace withSIX.Mini.Applications.Services
{
    public interface IGameSwitcher
    {
        Task<bool> SwitchGame(Guid id);
        Task UpdateGameState(Guid gameId, ContentQuery query = null);
    }

    public class GameSwitcher : IGameSwitcher, IApplicationService, IDisposable
    {
        private readonly IDbContextLocator _locator;
        readonly AsyncLock _lock = new AsyncLock();
        private readonly IGameLocker _locker;
        private readonly ISetupGameStuff _setup;
        private readonly IStateHandler _stateHandler;

        public GameSwitcher(IDbContextLocator locator, ISetupGameStuff setup, IGameLocker locker,
            IStateHandler stateHandler) {
            _locator = locator;
            _setup = setup;
            _locker = locker;
            _stateHandler = stateHandler;
        }

        public void Dispose() {
            _lock.Dispose();
        }

        public async Task UpdateGameState(Guid gameId, ContentQuery query = null) {
            _stateHandler.SelectedGameId = gameId;

            var gameContext = _locator.GetGameContext();
            var game = await gameContext.FindGameOrThrowAsync(gameId).ConfigureAwait(false);
            if (query == null)
                game.CleanupContent();

            await _setup.HandleGameContentsWhenNeeded(query, game.Id).ConfigureAwait(false);
        }

        public async Task<bool> SwitchGame(Guid id) {
            using (await _lock.LockAsync().ConfigureAwait(false)) {
                if (_stateHandler.SelectedGameId == id)
                    return false;

                using (await _locker.ConfirmLock(id).ConfigureAwait(false))
                    await UpdateGameState(id).ConfigureAwait(false);

                return true;
            }
        }
    }
}