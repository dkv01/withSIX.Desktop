// <copyright company="SIX Networks GmbH" file="ApiContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using SN.withSIX.Core;
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

        public ApiContext(ILocalCache cache) {
            _cache = cache;
        }

        public async Task<List<ModClientApiJsonV3WithGameId>> GetMods(Guid gameId, string version, IEnumerable<Guid> desiredContent = null) {
            var uri = new Uri(apiCdnHost, $"/api/v3/mods-{gameId}.json.gz");
            var versionKey = gameId + "_version";
            var uriKey = gameId + "_mods_v3";
            var v = await _cache.GetOrCreateObject(versionKey, () => "-1");
            List<ModClientApiJsonV3WithGameId> result;
            if (v != version) {
                result = await Retrieve(gameId, version, uri).ConfigureAwait(false);
                await
                    _cache.InsertObject(uriKey, result);
            } else
                result = await _cache.GetOrFetchObject(uriKey, () => Retrieve(gameId, version, uri));
            await _cache.InsertObject(versionKey, version);
            return desiredContent != null ? result.Where(x => desiredContent.Contains(x.Id)).ToList() : result;
        }

        public Task<ApiHashes> GetHashes(Guid gameId)
            => Tools.Transfer.GetJson<ApiHashes>(new Uri(apiCdnHost, $"/api/v3/hashes-{gameId}.json.gz"));

        private async Task<List<ModClientApiJsonV3WithGameId>> Retrieve(Guid gameId, string version, Uri uri) {
            var u = await new Uri(uri + $"?v={version}").GetJson<List<ModClientApiJsonV3WithGameId>>();
            // TODO: We dont actually need to store these as we can restore them..
            foreach (var c in u)
                c.GameId = gameId;

            // TODO: Handle these special dependencies on the server side!
            if (gameId != GameGuids.Arma3)
                return u;

            var mods = u.ToDictionary(x => x.Id, x => x);
            var backMods =
                await new Uri(apiCdnHost, "/api/v2/mods.json.gz").GetJson<List<ModDtoV2>>().ConfigureAwait(false);
            foreach (var m in backMods.Where(x => mods.ContainsKey(x.Id)))
                mods[m.Id].Tags = m.Tags;

            return u;
        }
    }
}