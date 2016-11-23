// <copyright company="SIX Networks GmbH" file="SetupGameStuff.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Core.Helpers;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Attributes;
using withSIX.Mini.Core.Games.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Infra.Data.Services
{
    public class SetupGameStuff : IInfrastructureService, ISetupGameStuff, IDisposable
    {
        private readonly IDbContextFactory _factory;
        private readonly IGameLocker _gameLocker;
        readonly IDbContextLocator _locator;
        readonly INetworkContentSyncer _networkContentSyncer;
        private readonly IStateHandler _stateHandler;
        //private readonly ICacheManager _cacheMan;
        private readonly TimerWithElapsedCancellationAsync _timer;
        private readonly GameFactory _gameFactory;

        public SetupGameStuff(IDbContextLocator locator, IDbContextFactory factory,
            INetworkContentSyncer networkContentSyncer, /* ICacheManager cacheMan, */
            IGameLocker gameLocker, IStateHandler stateHandler, IAssemblyService ass) {
            _locator = locator;
            _factory = factory;
            _networkContentSyncer = networkContentSyncer;
            //_cacheMan = cacheMan;
            _gameLocker = gameLocker;
            _stateHandler = stateHandler;
            _timer = new TimerWithElapsedCancellationAsync(TimeSpan.FromMinutes(30), onElapsedNonBool: OnElapsed);
            _gameFactory = new GameFactory(ass);
        }

        public static IDictionary<Type, GameAttribute> GameSpecs { get; private set; }

        public void Dispose() => _timer.Dispose();

        public Task Initialize() {
            GameSpecs = _gameFactory.GetGameTypesWithAttribute();
            return Migrate();
        }

        public Task HandleGameContentsWhenNeeded(IReadOnlyCollection<Guid> gameIds,
            ContentQuery query = null) => HandleGameContents(gameIds, query);

        async Task HandleGameContentsWhenNeededIndividualLock(params Guid[] tryGameIds) {
            foreach (var g in tryGameIds) {
                using (var i = await TryLock(g).ConfigureAwait(false))
                    if (i == null)
                        continue;
                using (var scope = _factory.Create()) {
                    await HandleGameContents(gameIds: g).ConfigureAwait(false);
                    await scope.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task<GameLockInfo> TryLock(Guid id) {
            try {
                return await _gameLocker.ConfirmLock(id).ConfigureAwait(false);
            } catch (AlreadyLockedException) {}
            return null;
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

        async Task Migrate() {
            var gc = _locator.GetGameContext();
            await HandleMissingGames(gc).ConfigureAwait(false);
            var migrated = await gc.Migrate(GetMigrations()).ConfigureAwait(false);
            //if (migrated) await _cacheMan.Vacuum().ConfigureAwait(false);
            //await Task.Run(() => gc.Migrate()).ConfigureAwait(false);
        }

        private static List<Migration> GetMigrations() => new List<Migration> {new Migration1()};

        async Task HandleMissingGames(IGameContext gc) {
            var newGames = new List<Game>();
            foreach (var g in GameSpecs) {
                if (!await gc.GameExists(g.Value.Id).ConfigureAwait(false))
                    newGames.Add(_gameFactory.CreateGame(g.Key, g.Value));
            }
            foreach (var ng in newGames)
                gc.Games.Add(ng);
            if (newGames.Any())
                await gc.SaveChanges().ConfigureAwait(false);
        }

        Task HandleGameContents(ContentQuery query = null, params Guid[] gameIds)
            => HandleGameContents(gameIds, query);

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
            return
                _networkContentSyncer.SyncCollections(games.SelectMany(x => x.SubscribedCollections).ToArray(), false);
        }

        class GameFactory
        {
            static readonly TypeInfo gameType = typeof(Game).GetTypeInfo();
            private readonly IAssemblyService _ass;

            public GameFactory(IAssemblyService ass) {
                _ass = ass;
            }

            public Game CreateGame(Type type, GameAttribute attr)
                => (Game) Activator.CreateInstance(type, attr.Id, CreateGameSettings(type));

            GameSettings CreateGameSettings(Type gt)
                => (GameSettings) Activator.CreateInstance(GetSettingsModelType(gt));

            Type GetSettingsModelType(Type x) {
                var typeName = MapToSettingsType(x);
                var type = x.GetTypeInfo().Assembly.GetType(typeName);
                if (type == null)
                    throw new InvalidOperationException("Cannot find the SettingsModelType required for " + x);
                return type;
            }

            string MapToSettingsType(Type x) => x.FullName.Replace("Game", "GameSettings");

            public IDictionary<Type, GameAttribute> GetGameTypesWithAttribute()
                => FindGameTypes().ToDictionary(x => x, x => x.GetTypeInfo().GetCustomAttribute<GameAttribute>());

            // TODO: Get the assemblies injected instead?
            IEnumerable<Type> FindGameTypes() => _ass.GetAllAssemblies()
                .Where(x => x.GetName().Name.StartsWith("withSIX.Mini.Plugin."))
                .SelectMany(x => _ass.GetTypes(x))
                .Where(IsGameType);

            bool IsGameType(Type y) {
                var x = y.GetTypeInfo();
                return !x.IsInterface && !x.IsAbstract && gameType.IsAssignableFrom(x) &&
                       (x.GetCustomAttribute<GameAttribute>() != null);
            }
        }

        class Migration1 : Migration
        {
            public override async Task Migrate(IGameContext gc) {
                await gc.LoadAll().ConfigureAwait(false);
                foreach (var g in gc.Games) {
                    g.CleanupContent();
                    g.MigrateContents();
                }
            }
        }
    }
}