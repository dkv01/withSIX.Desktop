// <copyright company="SIX Networks GmbH" file="GameContextJsonImplementation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Cache;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    // TODO: How to deal with damn circular references problems.
    // Like mbg_buildings2_eu was referencing itself as a dependency..
    // No error on serialization, but error on deserialization! Will cause major headache!
    // For now remedy by using lazy dependencies?? E.g serialize id's, and deserialize back to objects on a second run? juk!
    // (BTW I do believe we do catch other types of circular references, like depa, requires depb, requires depa, etc??)
    public class GameContextJsonImplementation : GameContext
    {
        static readonly Encoding encoding = Encoding.UTF8;
        public static JsonSerializerSettings Settings { get; set; }
        readonly ILocalCache _cache;

        public GameContextJsonImplementation(ILocalCache cache) {
            _cache = cache;
        }

        //public override async Task Migrate() {}

        public override async Task LoadAll(bool skip = false) {
            using (this.Bench()) {
                var gamesToAdd = await
                    Task.WhenAll(
                        GetSupportedGameIds()
                            .Where(x => !Games.Select(g => g.Id).Contains(x))
                            .Select(x => RetrieveGame(x, skip))).ConfigureAwait(false);
                Games.AddRange(gamesToAdd.Where(x => x != null));
            }
        }

        public override async Task<bool> Migrate(List<Migration> migrations) {
            var key = "____migration_version";
            var migrationId = await _cache.GetOrCreateObject(key, () => 0);
            var newMigrationId = migrations.Count;
            if (newMigrationId > migrationId) {
                foreach (var m in migrations.Skip(migrationId))
                    await m.Migrate(this).ConfigureAwait(false);
                // TODO: This should be a transaction :)
                await SaveChanges().ConfigureAwait(false);
                await _cache.InsertObject(key, newMigrationId);
                return true;
            }
            return false;
        }

        public override async Task Load(Guid gameId) {
            if (!GetSupportedGameIds().Contains(gameId))
                throw new NotFoundException($"The specified game is unknown/not supported {gameId}");
            using (this.Bench(gameId.ToString())) {
                if (!Games.Select(x => x.Id).Contains(gameId))
                    Games.Add(await RetrieveGame(gameId).ConfigureAwait(false));
            }
        }

        private static IEnumerable<Guid> GetSupportedGameIds() => SetupGameStuff.GameSpecs
            .Select(x => x.Value.Id);

        public override async Task<bool> GameExists(Guid gameId) {
            var r = await _cache.GetCreatedAt(GetCacheKey(gameId));
            return r != null;
        }

        async Task<Game> RetrieveGame(Guid gameId, bool skip = false) {
            Game game;
            try {
                var jsonStr = encoding.GetString(await _cache.Get(GetCacheKey(gameId)));
                game = JsonConvert.DeserializeObject<Game>(jsonStr,
                    Settings);
            } catch (KeyNotFoundException) {
                if (skip)
                    return null;
                throw new NotFoundException("Item with ID not found: " + gameId);
            }
            FixUpDependencies(game.NetworkContent.ToArray());
            return game;
        }

        // TODO: Be specific about which games dependencies can be taken from, like A3, could accesss the previous games content etc.
        internal static void FixUpDependencies(IReadOnlyCollection<NetworkContent> nc) {
            foreach (var c in nc) {
                c.ReplaceDependencies(c.InternalDependencies
                    .Select(x => new {Content = nc.Find(x.Id), x.Constraint})
                    .Where(x => x.Content != null)
                    .Select(x => new NetworkContentSpec(x.Content, x.Constraint)));
            }
        }

        static string GetCacheKey(Guid x) => "gamecontext_games-" + x;

        protected override async Task<int> SaveChangesInternal() {
            /*            await
                _cache.Insert(CacheKey,
                    encoding.GetBytes(JsonConvert.SerializeObject(this.MapTo<GameContextDto>(), Settings)));*/

            // TODO: Now we would have copies of various content spread out over the games that support content from other games :S
            foreach (var g in Games) {
                ConfirmConsistency(g);
                var jsonStr = JsonConvert.SerializeObject(g, Settings);
                await
                    _cache.Insert(GetCacheKey(g.Id),
                        encoding.GetBytes(jsonStr));
            }

            return -1; // TODO
        }

        private static void ConfirmConsistency(Game g) {
            //ConfirmGameContents(g);
        }

        private static void ConfirmGameContents(Game g) {
            if (g.Contents.GroupBy(x => x.Id).Any(x => x.Count() > 1)) {
                throw new InvalidOperationException(
                    $"DB Error: Tried to insert duplicate content for game: {g.Metadata.ShortName} [{g.Id}]");
            }
        }
    }
}