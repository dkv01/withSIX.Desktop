// <copyright company="SIX Networks GmbH" file="ApiContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Cache;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.WebApi;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Infra.Data
{
    public class ApiContext : IApiContext, IInfrastructureService
    {
        private static readonly Uri apiCdnHost = new Uri("http://withsix-api.azureedge.net");
        private readonly ILocalCache _cache;
        private readonly ConcurrentDictionary<string, List<ModClientApiJsonV3WithGameId>> _tempCache = new ConcurrentDictionary<string, List<ModClientApiJsonV3WithGameId>>();

        public ApiContext(ILocalCache cache) {
            _cache = cache;
        }

        public async Task<List<ModClientApiJsonV3WithGameId>> GetMods(Guid gameId, string version) {
            var localCacheKey = $"{gameId}_{version}";
            return _tempCache.ContainsKey(localCacheKey)
                ? _tempCache[localCacheKey]
                : (_tempCache[localCacheKey] = await GetOrFetchAndCache(gameId, version).ConfigureAwait(false));
        }

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

        public async Task<ApiHashes> GetHashes(Guid gameId)
            =>
                await
                    _cache.GetOrFetchObject("api_hashes_v3",
                        () => Tools.Transfer.GetJson<ApiHashes>(new Uri(apiCdnHost, $"/api/v3/hashes-{gameId}.json.gz")),
                        TimeSpan.FromSeconds(30).GetAbsoluteUtc());

        private async Task<List<ModClientApiJsonV3WithGameId>> Retrieve(Guid gameId, string version) {
            var content =
                await BuildUri(gameId, version).GetJson<List<ModClientApiJsonV3WithGameId>>().ConfigureAwait(false);
            foreach (var c in content)
                c.GameId = gameId;

            // TODO: Handle these special dependencies on the server side!
            if (gameId != GameGuids.Arma3)
                return content;

            var mods = content.ToDictionary(x => x.Id, x => x);
            var backMods =
                await
                    new Uri(apiCdnHost, $"/api/v2/mods.json.gz?v={version}").GetJson<List<ModDtoV2>>()
                        .ConfigureAwait(false);
            foreach (var m in backMods.Where(x => mods.ContainsKey(x.Id)))
                mods[m.Id].Tags = m.Tags;

            return content;
        }

        private static Uri BuildUri(Guid gameId, string version)
            => new Uri(apiCdnHost, $"/api/v3/mods-{gameId}.json.gz?v={version}");
    }
}