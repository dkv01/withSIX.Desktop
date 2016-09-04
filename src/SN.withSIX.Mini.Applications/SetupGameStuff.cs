// <copyright company="SIX Networks GmbH" file="SetupGameStuff.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Mini.Core.Games.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Steam.Core;

namespace SN.withSIX.Mini.Applications
{
    public interface ISetupGameStuff
    {
        Task Initialize();

        Task HandleGameContentsWhenNeeded(IReadOnlyCollection<Guid> gameIds, ContentQuery query = null);
    }

    public static class GameStuffExtensions
    {
        public static Task HandleGameContentsWhenNeeded(this ISetupGameStuff This,
            ContentQuery query = null, params Guid[] gameIds)
            => This.HandleGameContentsWhenNeeded(gameIds, query);
    }

    public class SetupGameStuff : IApplicationService, ISetupGameStuff, IDisposable
    {
        private readonly IDbContextFactory _factory;
        private readonly IGameLocker _gameLocker;
        private readonly IStateHandler _stateHandler;
        readonly IDbContextLocator _locator;
        readonly INetworkContentSyncer _networkContentSyncer;
        private readonly TimerWithElapsedCancellationAsync _timer;

        public SetupGameStuff(IDbContextLocator locator, IDbContextFactory factory,
            INetworkContentSyncer networkContentSyncer,
            IGameLocker gameLocker, IStateHandler stateHandler) {
            _locator = locator;
            _factory = factory;
            _networkContentSyncer = networkContentSyncer;
            _gameLocker = gameLocker;
            _stateHandler = stateHandler;
            _timer = new TimerWithElapsedCancellationAsync(TimeSpan.FromMinutes(30), onElapsedNonBool: OnElapsed);
        }

        public static IDictionary<Type, GameAttribute> GameSpecs { get; private set; }

        public void Dispose() => Dispose(true);

        public Task Initialize() => Migrate();

        public Task HandleGameContentsWhenNeeded(IReadOnlyCollection<Guid> gameIds,
            ContentQuery query = null) => HandleGameContents(gameIds, query);

        async Task HandleGameContentsWhenNeededIndividualLock(params Guid[] tryGameIds) {
            var lockedGameIds = new List<Guid>();
            try {
                lockedGameIds = await TryLockIndividualGames(tryGameIds).ConfigureAwait(false);
                if (lockedGameIds.Any()) {
                    using (var scope = _factory.Create()) {
                        await HandleGameContents(lockedGameIds).ConfigureAwait(false);
                        await scope.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            } finally {
                foreach (var id in lockedGameIds)
                    _gameLocker.ReleaseLock(id);
            }
        }

        private async Task<List<Guid>> TryLockIndividualGames(IEnumerable<Guid> gameIds) {
            var l = new List<Guid>();
            foreach (var id in gameIds) {
                try {
                    await _gameLocker.ConfirmLock(id).ConfigureAwait(false);
                    l.Add(id);
                } catch (AlreadyLockedException) {}
            }
            return l;
        }

        private async Task OnElapsed() {
            try {
                var gameId = _stateHandler.SelectedGameId;
                if (gameId == Guid.Empty)
                    return;
                await HandleGameContentsWhenNeededIndividualLock(gameId).ConfigureAwait(false);
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Error trying to sync game/content info");
            }
        }

        ~SetupGameStuff() {
            Dispose(false);
        }

        async Task Migrate() {
            var gc = _locator.GetGameContext();
            GameSpecs = GameFactory.GetGameTypesWithAttribute();
            await gc.Migrate().ConfigureAwait(false);
            //await Task.Run(() => gc.Migrate()).ConfigureAwait(false);
            await HandleMissingGames(gc).ConfigureAwait(false);
        }

        async Task HandleMissingGames(IGameContext gc) {
            await gc.LoadAll(true).ConfigureAwait(false);
            var newGames = GameSpecs
                .Where(x => !gc.Games.Select(g => g.Id).Contains(x.Value.Id))
                .Select(x => GameFactory.CreateGame(x.Key, x.Value))
                .ToArray();
            foreach (var ng in newGames)
                gc.Games.Add(ng);
            if (newGames.Any())
                await gc.SaveChanges().ConfigureAwait(false);
        }

        async Task HandleGameContents(IReadOnlyCollection<Guid> gameIds, ContentQuery query = null) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Handling game contents for {string.Join(", ", gameIds)}");

            var gc = _locator.GetGameContext();
            await gc.Load(gameIds).ConfigureAwait(false);

            var games = gc.Games.Where(x => gameIds.Contains(x.Id) && x.InstalledState.IsInstalled).ToArray();
            await
                _networkContentSyncer.SyncContent(games, query).ConfigureAwait(false);
            //if (shouldEmitEvents) await new StatusChanged(Status.Preparing, new ProgressInfo(progress: 50)).Raise().ConfigureAwait(false);
            await SynchronizeCollections(games).ConfigureAwait(false);
            foreach (var g in games)
                await g.RefreshState().ConfigureAwait(false);
        }

        Task SynchronizeCollections(IReadOnlyCollection<Game> games) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Syncing collections for games: {string.Join(", ", games.Select(x => x.Id))}");
            var contents = games.SelectMany(x => x.Contents).OfType<NetworkContent>().Distinct().ToArray();
            return
                _networkContentSyncer.SyncCollections(games.SelectMany(x => x.SubscribedCollections).ToArray(),
                    contents, false);
        }

        private void Dispose(bool isDisposing) {
            if (isDisposing)
                _timer.Dispose();
        }

        static class GameFactory
        {
            static readonly Type gameType = typeof (Game);

            public static Game CreateGame(Type type, GameAttribute attr)
                => (Game) Activator.CreateInstance(type, attr.Id, CreateGameSettings(type));

            static GameSettings CreateGameSettings(Type gt)
                => (GameSettings) Activator.CreateInstance(GetSettingsModelType(gt));

            static Type GetSettingsModelType(Type x) {
                var typeName = MapToSettingsType(x);
                var type = x.Assembly.GetType(typeName);
                if (type == null)
                    throw new InvalidOperationException("Cannot find the SettingsModelType required for " + x);
                return type;
            }

            static string MapToSettingsType(Type x) => x.FullName.Replace("Game", "GameSettings");

            public static IDictionary<Type, GameAttribute> GetGameTypesWithAttribute()
                => FindGameTypes().ToDictionary(x => x, x => x.GetCustomAttribute<GameAttribute>());

            static IEnumerable<Type> FindGameTypes() => AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(IsGameType);

            static bool IsGameType(Type x) => !x.IsInterface && !x.IsAbstract && gameType.IsAssignableFrom(x) &&
                                              x.GetCustomAttribute<GameAttribute>() != null;
        }
    }
}