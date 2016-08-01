// <copyright company="SIX Networks GmbH" file="ContentApiHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Mappers;
using Newtonsoft.Json;
using withSIX.Api.Models.Content;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Play.Core.Games.Legacy.Missions;
using SN.withSIX.Play.Core.Games.Legacy.Mods;
using SN.withSIX.Play.Core.Games.Services.Infrastructure;
using SN.withSIX.Play.Core.Options;
using SN.withSIX.Play.Infra.Api.ContentApi.Dto;
using withSIX.Api.Models.Content.v2;

namespace SN.withSIX.Play.Infra.Api.ContentApi
{
    static class ApiHashesExtension
    {
        // TODO: Horrible implementation, need to remain in sync with _repositoreis of Api!!
        public static List<string> ToList(this ApiHashes hashes) => new List<string> {
                hashes.Mods,
                hashes.ModSets,
                hashes.Missions
            };

        public static ApiHashes ToApiHashes(this List<string> hashes) => new ApiHashes {
            Mods = hashes[0],
            ModSets = hashes[1],
            Missions = hashes[2]
            // xx
        };
    }

    class ContentApiHandler : IContentApiHandler, IInfrastructureService
    {
        const string HashesJson = "hashes.json";
        readonly IApiLocalObjectCacheManager _cacheManager;
        readonly ContentApiRepository<MissionDto, Mission> _missionRepository;
        readonly ContentApiRepository<ModDto, Mod> _modRepository;
        readonly ContentApiRepository<ModSetDto, Collection> _modSetRepository;
        readonly IContentApiRepository[] _repositories;
        readonly ContentRestApi _rest;
        readonly UserSettings _settings;
        ApiHashes _localHashes;
        ApiHashes _remoteHashes;

        public ContentApiHandler(UserSettings settings, IApiLocalObjectCacheManager cacheManager) {
            _rest = new ContentRestApi();
            _settings = settings;
            _cacheManager = cacheManager;
            var mappingEngine = GetMapper();

            _remoteHashes = new ApiHashes();
            _localHashes = new ApiHashes();
            _modRepository = new ContentApiRepository<ModDto, Mod>(_rest, mappingEngine, cacheManager);
            _missionRepository = new ContentApiRepository<MissionDto, Mission>("mission", _rest, mappingEngine,
                cacheManager);
            _modSetRepository = new ContentApiRepository<ModSetDto, Collection>("mod_set", _rest, mappingEngine,
                cacheManager);
            _repositories = new IContentApiRepository[] {_modRepository, _modSetRepository, _missionRepository};
        }

        public bool Loaded { get; private set; }

        public async Task LoadFromDisk() {
            var loaded = true;
            var newHashes = new List<string>();
            foreach (var repo in _repositories) {
                if (!(await repo.TryLoadFromDisk().ConfigureAwait(false)))
                    loaded = false;
                newHashes.Add(repo.Hash);
            }

            Loaded = loaded;

            // This might be overzealous, but at least fixes the current issue (otherwise had to restart twice to fix current issue)
            _localHashes = newHashes.ToApiHashes(); //await GetHashes().ConfigureAwait(false);
        }

        public List<T> GetList<T>() => _repositories.First(x => x.GetType().GenericTypeArguments[1] == typeof (T))
            .GetValues().Cast<T>().ToList();

        public async Task<bool> LoadFromApi() {
            var data = (await _rest.GetDataAsync<ApiHashes>(HashesJson + ".gz").ConfigureAwait(false));
            return await LoadFromApi(data.Item1).ConfigureAwait(false);
        }

        public async Task<bool> LoadFromApi(ApiHashes hashes) {
            _remoteHashes = hashes;

            var remoteList = _remoteHashes.ToList();
            var localList = _localHashes.ToList();
            var newHashes = new List<string>();

            var changed = false;

            // Temporary doing in foreach loop because we load and map at the same time currently, while we intend for items to rely on eachother..
            var i = 0;
            foreach (var repo in _repositories) {
                var j = i++;
                // TODO: Horrible index based, needs to keep ToList and _repositories into sync!! :<
                var remoteHash = remoteList[j];
                var localHash = localList[j];
                if (remoteHash != localHash) {
                    await repo.LoadFromApi(remoteHash).ConfigureAwait(false);
                    changed = true;
                }
                newHashes.Add(repo.Hash);
            }
            var newHash = newHashes.ToApiHashes();
            await _cacheManager.SetObject(RepoHelper.GetFullApiPath(HashesJson), newHash.ToJson());
            _localHashes = newHash;

            return changed;
        }

        async Task<ApiHashes> GetHashes() {
            // TODO: Without ExHandling?
            // TODO: Don't save the JSON representation but our own? But then if we have a bug in the client we can have wrong data in the cache so..

            try {
                var data = await _cacheManager.GetObject<string>(RepoHelper.GetFullApiPath(HashesJson));
                return JsonConvert.DeserializeObject<ApiHashes>(data, ContentRestApi.JsonSettings);
            } catch (KeyNotFoundException) {
                return new ApiHashes();
            }
        }

        IMapper GetMapper() {
            var c = new MapperConfiguration(mapConfig => {

                mapConfig.SetupConverters();

                mapConfig.CreateMap<MissionDto, Mission>()
                    .ConstructUsing(input => _missionRepository.GetOrCreate(input.Id))
                    .ForMember(x => x.Name, opt => opt.MapFrom(src => src.PackageName))
                    .ForMember(x => x.Island, opt => opt.MapFrom(src => src.Map))
                    .ForMember(x => x.FullName, opt => opt.MapFrom(src => src.Name))
                    .AfterMap(AfterMapping);

                mapConfig.CreateMap<ModDto, Mod>()
                    .ConstructUsing(input => _modRepository.GetOrCreate(input.Id))
                    .ForMember(x => x.Name, opt => opt.MapFrom(src => src.PackageName))
                    .ForMember(x => x.FullName, opt => opt.MapFrom(src => src.Name))
                    .ForMember(x => x.ModVersion, opt => opt.MapFrom(src => src.Version))
                    .ForMember(x => x.Version, opt => opt.Ignore())
                    /*
                .ForMember(x => x.Networks,
                    opt =>
                        opt.ResolveUsing(
                            src => src.Networks == null
                                ? new List<Network>()
                                : src.Networks.Select(
                                    n => _networkRepository.Get(n.Uuid))
                                    .Where(n => n != null)))
*/
                    .ForMember(x => x.Categories, opt => opt.ResolveUsing(GetModTagsOrDefault))
                    .ForMember(x => x.Aliases,
                        opt => opt.ResolveUsing(src => src.Aliases?.Split(';') ?? new string[0]))
                    .AfterMap(AfterMapping);

                mapConfig.CreateMap<ModSetDto, Collection>()
                    .ConstructUsing(input => _modSetRepository.GetOrCreate(input.Id))
                    .ForMember(x => x.GameId, opt => opt.ResolveUsing(x => x.GameUuid == Guid.Empty ? Collection.DefaultGameUuid : x.GameUuid))
                    .AfterMap((src, dst) => dst.IsFavorite = _settings.ModOptions.IsFavorite(dst));
            });
            return c.CreateMapper();
        }

        void AfterMapping(ModDto src, Mod dst) {
            dst.IsFavorite = _settings.ModOptions.IsFavorite(dst);
            if (src.ImagePath != null) {
                dst.Image =
                    Tools.Transfer.JoinUri(CommonUrls.UsercontentCdnProduction, src.ImagePath)
                        .ToString();
            }
        }

        void AfterMapping(MissionDto src, Mission dst) {
            dst.IsFavorite = _settings.MissionOptions.IsFavorite(dst);
            var list = new List<string>();
            dst.Type = MissionTypes.SpMission;
            if (src.Playability.HasFlag(Playability.Singleplayer))
                list.Add(MissionTypesHunan.SpMission);
            if (src.Playability.HasFlag(Playability.Multiplayer)) {
                dst.Type = MissionTypes.MpMission;
                list.Add(MissionTypesHunan.MpMission);
            }
            dst.Types = list.ToArray();
            if (src.ImagePath != null) {
                dst.Image =
                    Tools.Transfer.JoinUri(CommonUrls.UsercontentCdnProduction, src.ImagePath, "160x100.jpg")
                        .ToString();
            }
        }

        string[] GetModTagsOrDefault(ModDto src) {
            var tags = GetModTags(src).ToArray();
            return tags.Any() ? tags.Select(x => x.ToString()).ToArray() : new[] {Common.DefaultCategory};
        }

        IEnumerable<string> GetModTags(ModDto src) => src.Tags ?? new List<string>();
    }
}