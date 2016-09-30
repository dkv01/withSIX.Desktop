// <copyright company="SIX Networks GmbH" file="ContentApiRepository.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Newtonsoft.Json;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Infra.Api.ContentApi.Dto;

namespace withSIX.Play.Infra.Api.ContentApi
{
    public static class RepoHelper
    {
        public static string GetFullApiPath(string apiPath) => String.Join("/", "api", "v" + CommonUrls.ContentApiVersion, apiPath);
        public static string GetShortHash(string data) => GetShortHash(Encoding.UTF8.GetBytes(data));

        public static string GetShortHash(byte[] content) => Convert.ToBase64String(content.Sha1()).TrimEnd('=');
    }

    class ContentApiRepository<T, T2> : IContentApiRepository
        where T : class, ISyncBaseDto
        where T2 : class, ISyncBase
    {
        const string JsonExt = ".json";
        readonly string _apiPath;
        readonly IApiLocalObjectCacheManager _cacheManager;
        readonly string _fullApiPath;
        readonly IMapper _mappingEngine;
        readonly ContentRestApi _rest;

        public ContentApiRepository(string type, ContentRestApi rest, IMapper mappingEngine,
            IApiLocalObjectCacheManager cacheManager) {
            var multi = type.Pluralize();
            _apiPath = multi + JsonExt;
            _fullApiPath = RepoHelper.GetFullApiPath(_apiPath);
            _rest = rest;
            _mappingEngine = mappingEngine;
            _cacheManager = cacheManager;
            Items = new Dictionary<Guid, T2>();
        }

        public ContentApiRepository(ContentRestApi rest, IMapper mappingEngine,
            IApiLocalObjectCacheManager cacheManager) :
                this(typeof (T2).Name.ToUnderscore(), rest, mappingEngine, cacheManager) {}

        public Dictionary<Guid, T2> Items { get; private set; }

        public async Task<bool> TryLoadFromDisk() {
            var data = await LoadAndMapFromDisk().ConfigureAwait(false);
            if (data == null)
                return false;
            Items = MakeDictionary(data);
            return true;
        }

        public async Task LoadFromApi(string hash) {
            var data = await LoadAndMapFromApi(hash).ConfigureAwait(false);
            if (data != null)
                Items = MakeDictionary(data);
        }

        [Obsolete("nuts")]
        public IEnumerable GetValues() => Items.Values;

        public string Hash { get; private set; }

        async Task<IReadOnlyCollection<T2>> LoadAndMapFromDisk() => Map(await LoadFromDisk().ConfigureAwait(false));

        async Task<List<T>> LoadFromDisk() {
            // TODO: Without ExHandling?
            // TODO: Don't save the JSON representation but our own? But then if we have a bug in the client we can have wrong data in the cache so..
            // the other way around is that we can have bad json data in the cache...
            try {
                var data = await _cacheManager.GetObject<string>(_fullApiPath);
                Hash = RepoHelper.GetShortHash(data);
                return JsonConvert.DeserializeObject<List<T>>(data, ContentRestApi.JsonSettings);
            } catch (KeyNotFoundException) {
                return null;
            }
        }

        async Task<List<T>> LoadFromApiAndSaveToDisk(string hash) {
            var data = await _rest.GetDataAsync<List<T>>(_apiPath + ".gz?v=" + hash).ConfigureAwait(false);
            await SaveDataToDisk(data.Item2).ConfigureAwait(false);
            Hash = RepoHelper.GetShortHash(data.Item2);
            return data.Item1;
        }

        public T2 Get(Guid id) {
            lock (Items)
                return Items.ContainsKey(id) ? Items[id] : null;
        }

        public T2 GetOrCreate(Guid id) => Get(id) ?? (T2)Activator.CreateInstance(typeof(T2), id);

        async Task SaveDataToDisk(string data) {
            await _cacheManager.SetObject(_fullApiPath, data);
        }

        async Task<List<T2>> LoadAndMapFromApi(string hash) => Map(await LoadFromApiAndSaveToDisk(hash).ConfigureAwait(false));

        List<T2> Map(List<T> list) => _mappingEngine.Map<List<T2>>(list);

        static Dictionary<Guid, T2> MakeDictionary(IEnumerable<T2> data) => data.ToDictionary(x => x.Id, y => y);
    }
}