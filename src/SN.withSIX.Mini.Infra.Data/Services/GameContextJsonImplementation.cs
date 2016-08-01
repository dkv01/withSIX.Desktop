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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Cache;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Core.Games;

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
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
            // TODO: Very dangerous because we cant load/save when versions change?!? http://stackoverflow.com/questions/32245340/json-net-error-resolving-type-in-powershell-cmdlet
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            Error = OnError,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        }.SetDefaultConverters();
        readonly ILocalCache _cache;

        public GameContextJsonImplementation(ILocalCache cache) {
            _cache = cache;
        }

        static void OnError(object sender, ErrorEventArgs e) {
            e.ToString();
        }

        //public override async Task Migrate() {}

        public override async Task LoadAll(bool skip = false) {
            using (this.Bench()) {
                var gamesToAdd = await
                    Task.WhenAll(
                        SetupGameStuff.GameSpecs
                            .Select(x => x.Value.Id)
                            .Where(x => !Games.Select(g => g.Id).Contains(x))
                            .Select(x => RetrieveGame(x, skip)))
                        .ConfigureAwait(false);
                Games.AddRange(gamesToAdd.Where(x => x != null));
            }
        }

        public override async Task Load(Guid gameId) {
            using (this.Bench(gameId.ToString())) {
                if (!Games.Select(x => x.Id).Contains(gameId))
                    Games.Add(await RetrieveGame(gameId).ConfigureAwait(false));
            }
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
                var jsonStr = JsonConvert.SerializeObject(g, Settings);
                await
                    _cache.Insert(GetCacheKey(g.Id),
                        encoding.GetBytes(jsonStr));
            }

            return -1; // TODO
        }
    }
}