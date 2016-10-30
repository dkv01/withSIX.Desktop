// <copyright company="SIX Networks GmbH" file="ApiContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Akavache;
using withSIX.Api.Models.Games;
using withSIX.Core.Extensions;
using withSIX.Core.Infra.Cache;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Core;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Infra.Data.ApiModels;

namespace withSIX.Mini.Infra.Data
{
    public class ApiContext : IApiContext, IInfrastructureService
    {
        private static readonly Uri apiCdnHost = new Uri("http://withsix-api.azureedge.net");
        private readonly IApiLocalCache _cache;
        private readonly IW6Api _api;
        private readonly Dictionary<string, object> _tempCache = new Dictionary<string, object>();

        public ApiContext(IApiLocalCache cache, IW6Api api) {
            _cache = cache;
            _api = api;
        }

        public Task<List<ModClientApiJsonV3WithGameId>> GetMods(Guid gameId, string version)
            => GetOrCreateInTempCache($"{gameId}_{version}", () => GetOrFetchAndCache(gameId, version));

        public Task<ApiHashes> GetHashes(Guid gameId, CancellationToken ct)
            => GetOrCreateInTempCache($"{gameId}_hashes", () => GetApiHashes(gameId, ct));

        public Task<List<IPEndPoint>> GetOrAddServers(Guid gameId, Func<Task<List<IPEndPoint>>> factory)
            => _cache.GetOrFetchObject($"{gameId}_servers", factory, TimeSpan.FromMinutes(5).GetAbsoluteUtcOffset())
                .ToTask();

        private async Task<T> GetOrCreateInTempCache<T>(string localCacheKey, Func<Task<T>> creator)
            => (T) (_tempCache.ContainsKey(localCacheKey)
                ? _tempCache[localCacheKey]
                : (_tempCache[localCacheKey] = await creator().ConfigureAwait(false)));

        private async Task<List<ModClientApiJsonV3WithGameId>> GetOrFetchAndCache(Guid gameId, string version) {
            List<ModClientApiJsonV3WithGameId> result;
            var versionKey = gameId + "_version";
            var uriKey = gameId + "_mods_v3";

            var v = await _cache.GetOrCreateObject(versionKey, () => "-1");
            if (v != version) {
                result = await Retrieve(gameId, version).ConfigureAwait(false);
                await _cache.InsertObject(uriKey, result);
            } else
                result = await _cache.GetOrFetchObject(uriKey, () => Retrieve(gameId, version));
            await _cache.InsertObject(versionKey, version);
            return result;
        }

        private Task<ApiHashes> GetApiHashes(Guid gameId, CancellationToken ct)
            => _api.Hashes(gameId, ct);

        private async Task<List<ModClientApiJsonV3WithGameId>> Retrieve(Guid gameId, string version) {
            var content =
                await BuildUri(gameId, version).GetJson<List<ModClientApiJsonV3WithGameId>>().ConfigureAwait(false);
            await HandleSpecialCases(content, gameId, version).ConfigureAwait(false);
            return content;
        }

        private async Task HandleSpecialCases(IReadOnlyCollection<ModClientApiJsonV3WithGameId> content, Guid gameId,
            string version) {
            // The api does not transfer game ids to save bw
            foreach (var c in content)
                c.GameId = gameId;

            // TODO: Handle these special dependencies on the server side!
            if (gameId != GameGuids.Arma3)
                return;

            var mods = content.ToDictionary(x => x.Id, x => x);
            var backMods =
                await
                    new Uri(apiCdnHost, $"/api/v2/mods.json.gz?v={version}").GetJson<List<ModDtoV2>>()
                        .ConfigureAwait(false);
            foreach (var m in backMods.Where(x => mods.ContainsKey(x.Id)))
                mods[m.Id].Tags = m.Tags;
        }

        private static Uri BuildUri(Guid gameId, string version)
            => new Uri(apiCdnHost, $"/api/v3/mods-{gameId}.json.gz?v={version}");
    }
}