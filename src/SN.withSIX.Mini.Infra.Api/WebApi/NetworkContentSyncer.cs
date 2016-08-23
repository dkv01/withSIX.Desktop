// <copyright company="SIX Networks GmbH" file="NetworkContentSyncer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Social;
using SN.withSIX.Sync.Core;
using SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo;
using SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo.dtos;
using SN.withSIX.Sync.Core.Packages;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Exceptions;
using ApiHashes = SN.withSIX.Mini.Core.Games.ApiHashes;
using ContentGuidSpec = withSIX.Api.Models.Content.v3.ContentGuidSpec;

namespace SN.withSIX.Mini.Infra.Api.WebApi
{
    public class NetworkContentSyncer : IInfrastructureService, INetworkContentSyncer
    {
        private static readonly Uri apiCdnHost = new Uri("http://withsix-api.azureedge.net");
        private readonly CollectionSyncer _collectionSyncer;
        readonly IDbContextLocator _locator;

        public NetworkContentSyncer(IDbContextLocator locator) {
            _locator = locator;
            _collectionSyncer = new CollectionSyncer(locator);
        }

        public async Task SyncContent(IReadOnlyCollection<Game> games) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Syncing content for games: {string.Join(", ", games.Select(x => x.Id))}");
            foreach (var g in games)
                await ProcessGame(g).ConfigureAwait(false);
        }

        public Task SyncCollections(IReadOnlyCollection<SubscribedCollection> collections,
            IReadOnlyCollection<NetworkContent> content, bool countCheck = true)
            => _collectionSyncer.SyncCollections(collections, content, countCheck);

        public Task<IReadOnlyCollection<SubscribedCollection>> GetCollections(Guid gameId,
            IReadOnlyCollection<Guid> collectionIds, IReadOnlyCollection<NetworkContent> content)
            => _collectionSyncer.GetCollections(gameId, collectionIds, content);

        async Task<List<ModDtoV2>> DownloadContentListV2(IEnumerable<Guid> gameIds, ApiHashes hashes) {
            var mods =
                await
                    Tools.Transfer.GetJson<List<ModDtoV2>>(
                        new Uri(apiCdnHost, "/api/v2/mods.json.gz?v=" + hashes.Mods))
                        .ConfigureAwait(false);
            return mods.Where(x => gameIds.Contains(x.GameId)).ToList();
        }

        Task<List<ModClientApiJson>> DownloadContentListV3(Guid gameId, ApiHashes hashes) =>
            Tools.Transfer.GetJson<List<ModClientApiJson>>(
                new Uri(apiCdnHost, $"/api/v3/mods-{gameId}.json.gz?v=" + hashes.Mods));

        async Task ProcessGame(Game game) {
            var invalidContent = game.Contents.Where(x => x.GameId == Guid.Empty).ToArray();
            if (invalidContent.Any())
                invalidContent.ForEach(x => x.FixGameId(game.Id));

            var stats = await GetHashStats(game).ConfigureAwait(false);
            if (!stats.ShouldSyncBecauseHashes && !stats.ShouldSyncBecauseVersion)
                return;
            var gameContent = await GetContent(game, stats.Hashes).ConfigureAwait(false);
            ProcessContents(game, gameContent);
            ProcessLocalContent(game);
            game.RefreshCollections();

            var si = game.SyncInfo;
            var dt = Tools.Generic.GetCurrentUtcDateTime;
            si.ApiHashes = stats.Hashes;
            si.LastSync = dt;
            si.LastSyncVersion = Consts.SyncVersion;
        }

        private async Task<HashStats> GetHashStats(Game game) {
            var syncInfo = game.SyncInfo;
            var localHashes = syncInfo.ApiHashes;
            var hashes = await GetHashesV3(game.Id).ConfigureAwait(false);

            var hashStats = new HashStats {
                Hashes = hashes,
                ShouldSyncBecauseHashes = localHashes == null || localHashes.Mods != hashes.Mods,
                /*
                ShouldSyncBecauseTime = syncInfo.LastSync.ToLocalTime() <
                                        Tools.Generic.GetCurrentUtcDateTime.ToLocalTime()
                                            .Subtract(TimeSpan.FromMinutes(10)),
                                            */
                ShouldSyncBecauseVersion = syncInfo.LastSyncVersion != Consts.SyncVersion
            };
            return hashStats;
        }

        Task<ApiHashes> GetHashesV3(Guid gameId)
            => Tools.Transfer.GetJson<ApiHashes>(new Uri(apiCdnHost, $"/api/v3/hashes-{gameId}.json.gz"));

        private async Task<List<ModClientApiJsonV3WithGameId>> GetContent(Game game, ApiHashes latestHashes) {
            var compatGameIds = game.GetCompatibleGameIds();
            var ctx = _locator.GetGameContext();
            await ctx.Load(compatGameIds).ConfigureAwait(false);

            var mods = new List<ModClientApiJsonV3WithGameId>();
            foreach (var c in compatGameIds) {
                var cMods = await DownloadContentListV3(c, latestHashes).ConfigureAwait(false);
                mods.AddRange(
                    cMods.Select(
                        x =>
                            MappingExtensions.Mapper.Map<ModClientApiJson, ModClientApiJsonV3WithGameId>(x,
                                opts => opts.AfterMap((src, dest) => dest.GameId = c))));
            }
            if (game.Id == GameUuids.Arma3) {
                // TODO: Handle these special dependencies on the server side!
                var backMods = await DownloadContentListV2(compatGameIds, latestHashes).ConfigureAwait(false);
                foreach (var m in backMods) {
                    var nm = mods.FirstOrDefault(x => x.Id == m.Id);
                    if (nm == null)
                        continue;
                    nm.Tags = m.Tags;
                }
            }
            return mods;
        }

        static void ProcessContents(Game game, IEnumerable<ModClientApiJsonV3WithGameId> contents) {
            // TODO: If we timestamp the DTO's, and save the timestamp also in our database,
            // then we can simply update data only when it has actually changed and speed things up.
            // The only thing to remember is when there are schema changes / new fields etc, either all timestamps need updating
            // or the syncer needs to take it into account..
            var mapping = new Dictionary<ModClientApiJsonV3WithGameId, ModNetworkContent>();
            UpdateContents(game, contents, mapping);
            HandleDependencies(game, mapping);
        }

        static void UpdateContents(Game game, IEnumerable<ModClientApiJsonV3WithGameId> contents,
            IDictionary<ModClientApiJsonV3WithGameId, ModNetworkContent> content) {
            var newContent = new List<ModNetworkContent>();
            var defaultTags = new List<string>();
            foreach (
                var c in
                    contents.Select(
                        x => new {DTO = x, Existing = game.NetworkContent.OfType<ModNetworkContent>().Find(x.Id)})
                        .OrderByDescending(x => x.DTO.GameId == game.Id)) {
                var theDto = c.DTO;
                var theGame = SetupGameStuff.GameSpecs.Select(x => x.Value).FindOrThrow(c.DTO.GameId);
                if (c.DTO.GameId != game.Id) {
                    // Make a copy of the DTO, and clone the list, because otherwise the changes will bleed through to other games...
                    theDto = c.DTO.MapTo<ModClientApiJsonV3WithGameId>();
                    theDto.Dependencies = theDto.Dependencies.ToList();
                    var cMods = game.GetCompatibilityMods(theDto.PackageName, theDto.Tags ?? defaultTags);
                    foreach (var m in cMods) {
                        var theM = content.Keys.FirstOrDefault(x => x.PackageName.Equals(m, StringComparison.CurrentCultureIgnoreCase));
                        if (theM != null && theDto.Dependencies.All(x => x.Id != theM.Id))
                            theDto.Dependencies.Add(new ContentGuidSpec {Id = theM.Id});
                    }
                }
                if (c.Existing == null) {
                    var nc = theDto.MapTo<ModNetworkContent>();
                    newContent.Add(nc);
                    content[theDto] = nc;
                } else {
                    c.Existing.UpdateVersionInfo(theDto.LatestStableVersion, theDto.UpdatedVersion);
                    theDto.MapTo(c.Existing);
                    content[theDto] = c.Existing;
                }
                if (c.DTO.GameId != game.Id) {
                    content[theDto].HandleOriginalGame(c.DTO.GameId, theGame.Slug, game.Id);
                }
            }
            game.Contents.AddRange(newContent);
        }

        static void HandleDependencies(Game game, Dictionary<ModClientApiJsonV3WithGameId, ModNetworkContent> content) {
            foreach (var nc in content)
                HandleDependencies(nc, game.NetworkContent.OfType<ModNetworkContent>());
        }

        // TODO: catch frigging circular reference mayhem!
        // http://stackoverflow.com/questions/16472958/json-net-silently-ignores-circular-references-and-sets-arbitrary-links-in-the-ch
        // http://stackoverflow.com/questions/21686499/how-to-restore-circular-references-e-g-id-from-json-net-serialized-json
        static void HandleDependencies(KeyValuePair<ModClientApiJsonV3WithGameId, ModNetworkContent> nc,
            IEnumerable<ModNetworkContent> networkContent) {
            nc.Value.ReplaceDependencies(nc.Key.Dependencies.Select(
                d => networkContent.FirstOrDefault(x => x.Id == d.Id))
                // TODO: Find out why we would have nulls..
                .Where(x => x != null)
                .Select(x => new NetworkContentSpec(x)));
        }

        static void HandleDependencies(KeyValuePair<ModClientApiJson, ModNetworkContent> nc,
            IEnumerable<ModNetworkContent> networkContent) {
            nc.Value.ReplaceDependencies(
                nc.Key.Dependencies.Select(
                    d => new {d.Constraint, Content = networkContent.FirstOrDefault(x => x.Id == d.Id)})
                    // TODO: Find out why we would have nulls..
                    .Where(x => x.Content != null)
                    .Select(x => new NetworkContentSpec(x.Content, x.Constraint)));
        }

        static void ProcessLocalContent(Game game) {
            var networkContents = game.NetworkContent.ToArray();
            var dict = game.LocalContent.Select(
                x =>
                    new {
                        x,
                        Nc =
                            networkContents.FirstOrDefault(
                                nc => nc.PackageName.Equals(x.PackageName, StringComparison.CurrentCultureIgnoreCase))
                    })
                .ToDictionary(x => x.x, x => x.Nc);

            var gameInstalled = game.InstalledState.IsInstalled;
            foreach (var c in dict.Where(c => c.Value != null)) {
                var version = gameInstalled ? GetVersion(game.ContentPaths.Path) : null;
                c.Value.Installed(version ?? (c.Key.IsInstalled() ? c.Key.InstallInfo.Version : null), version != null);
                ReplaceLocalContentInCollection(game, c);
                game.Contents.Remove(c.Key);
            }
        }

        private static string GetVersion(IAbsoluteDirectoryPath path) {
            var v = Package.ReadSynqInfoFile(path);
            return v?.VersionData;
        }

        private static void ReplaceLocalContentInCollection(Game game, KeyValuePair<LocalContent, NetworkContent> c) {
            foreach (var col in game.Collections) {
                var existing = col.Contents.FirstOrDefault(x => x.Content == c.Key);
                if (existing == null)
                    continue;
                col.Contents.Remove(existing);
                col.Contents.Add(new ContentSpec(c.Value, existing.Constraint));
            }
        }


        private class HashStats
        {
            public ApiHashes Hashes { get; set; }
            public bool ShouldSyncBecauseHashes { get; set; }
            //public bool ShouldSyncBecauseTime { get; set; }
            public bool ShouldSyncBecauseVersion { get; set; }
        }

        public class CollectionSyncer
        {
            private readonly IDbContextLocator _locator;

            public CollectionSyncer(IDbContextLocator locator) {
                _locator = locator;
            }

            public async Task SyncCollections(IReadOnlyCollection<SubscribedCollection> collections,
                IReadOnlyCollection<NetworkContent> content, bool countCheck = true) {
                var contents =
                    await
                        DownloadCollections(
                            collections.GroupBy(x => x.GameId)
                                .Select(x => new Tuple<Guid, List<Guid>>(x.Key, x.Select(t => t.Id).ToList())))
                            .ConfigureAwait(false);

                if (countCheck && contents.Count < collections.Count)
                    throw new NotFoundException("Could not find all requested collections");

                foreach (var c in contents.Select(x => new {x, Col = collections.FindOrThrow(x.Id)})) {
                    c.x.MapTo(c.Col);
                    await HandleContent(content, c.Col, c.x).ConfigureAwait(false);
                    c.Col.UpdateState();
                }
            }

            private async Task<IReadOnlyCollection<GroupContent>> GetGroupContent(Content col) {
                if (!col.GroupId.HasValue)
                    return new GroupContent[0];
                var group = new Group(col.GroupId.Value, "UNKNOWN");
                await @group.Load(await GetToken().ConfigureAwait(false)).ConfigureAwait(false);
                return @group.Content;
            }

            private static async Task<CustomRepo[]> GetRepositories(IHaveRepositories col) {
                var repositories =
                    col.Repositories.Select(r => new CustomRepo(CustomRepo.GetRepoUri(new Uri(r)))).ToArray();
                foreach (var r in repositories) {
                    try {
                        await r.Load(SyncEvilGlobal.StringDownloader, r.Uri).ConfigureAwait(false);
                    } catch (Exception ex) {
                        MainLog.Logger.FormattedWarnException(ex, "Error while processing repo");
                    }
                }

                return repositories.Where(x => x.Loaded).ToArray();
            }

            private async Task HandleContent(IReadOnlyCollection<NetworkContent> content, NetworkCollection col,
                CollectionModelWithLatestVersion c, List<NetworkCollection> collections = null) {
                if (collections == null)
                    collections = new List<NetworkCollection> {col};
                var collectionSpecs = await ProcessEmbeddedCollections(content, c, collections).ConfigureAwait(false);
                var modSpecs = await ProcessMods(content, col, c).ConfigureAwait(false);
                col.ReplaceContent(modSpecs.Concat(collectionSpecs));
            }

            private async Task<IReadOnlyCollection<ContentSpec>> ProcessMods(
                IReadOnlyCollection<NetworkContent> content,
                NetworkCollection col,
                CollectionModelWithLatestVersion c) {
                var customRepos = await GetRepositories(col).ConfigureAwait(false);
                var groupContent = await GetGroupContent(col).ConfigureAwait(false);
                return c.LatestVersion
                    .Dependencies
                    .Where(x => x.DependencyType == DependencyType.Package)
                    .Select(
                        x =>
                            new {
                                Content = ConvertToGroupOrRepoContent(x, col, customRepos, groupContent, content) ??
                                          ConvertToContentOrLocal(x, col, content), // temporary
                                x.Constraint
                            })
                    .Where(x => x.Content != null)
                    .Select(x => new ContentSpec(x.Content, x.Constraint))
                    .ToArray();
            }

            private async Task<IEnumerable<ContentSpec>> ProcessEmbeddedCollections(
                IReadOnlyCollection<NetworkContent> content, CollectionModelWithLatestVersion c,
                List<NetworkCollection> collections) {
                var embeddedCollections =
                    c.LatestVersion.Dependencies.Where(x => x.DependencyType == DependencyType.Collection).ToArray();
                if (!embeddedCollections.Any())
                    return Enumerable.Empty<ContentSpec>();

                var todoCols =
                    embeddedCollections.Where(x => collections.All(co => co.Id != x.CollectionDependencyId.Value))
                        .ToArray();
                var cols = await
                    RetrieveCollections(c.GameId, todoCols.Select(x => x.CollectionDependencyId.Value))
                        .ConfigureAwait(false);

                var buildCollections = new List<ContentSpec>();
                foreach (var ec in embeddedCollections) {
                    var contentSpec =
                        await GetContentSpec(content, collections, todoCols, ec, cols).ConfigureAwait(false);
                    buildCollections.Add(contentSpec);
                }
                return buildCollections;
            }

            private async Task<ContentSpec> GetContentSpec(IReadOnlyCollection<NetworkContent> content,
                List<NetworkCollection> collections, CollectionVersionDependencyModel[] todoCols,
                CollectionVersionDependencyModel ec, List<CollectionModelWithLatestVersion> cols) {
                if (todoCols.Contains(ec)) {
                    return
                        await ProcessEmbeddedCollection(content, cols, ec, collections).ConfigureAwait(false);
                }
                return new ContentSpec(collections.First(co => co.Id == ec.CollectionDependencyId.Value),
                    ec.Constraint);
            }

            private async Task<ContentSpec> ProcessEmbeddedCollection(IReadOnlyCollection<NetworkContent> content,
                IEnumerable<CollectionModelWithLatestVersion> cols, CollectionVersionDependencyModel ec,
                List<NetworkCollection> collections) {
                var rc = cols.First(c2 => c2.Id == ec.CollectionDependencyId.Value);
                var conv = rc.MapTo<SubscribedCollection>();
                collections.Add(conv);
                // TODO: Allow parent repos to be used for children etc? :-)
                await HandleContent(content, conv, rc, collections).ConfigureAwait(false);
                conv.UpdateState();
                return new ContentSpec(conv, ec.Constraint);
            }

            private static Content ConvertToGroupOrRepoContent(CollectionVersionDependencyModel x, Collection col,
                IReadOnlyCollection<CustomRepo> customRepos, IEnumerable<GroupContent> groupContent,
                IReadOnlyCollection<NetworkContent> content) {
                var gc =
                    groupContent.FirstOrDefault(
                        c => c.PackageName.Equals(x.Dependency, StringComparison.CurrentCultureIgnoreCase));
                if (gc != null)
                    // TODO: dependencies etc
                    return new ModNetworkGroupContent(gc.Id, gc.PackageName, gc.PackageName, gc.GameId);
                return HandleRepoContent(x, col, customRepos, content);
            }

            private static Content HandleRepoContent(CollectionVersionDependencyModel x, Collection col,
                IReadOnlyCollection<CustomRepo> customRepos, IReadOnlyCollection<NetworkContent> content) {
                var repo = customRepos.FirstOrDefault(r => r.HasMod(x.Dependency));
                if (repo == null)
                    return null;
                var repoContent = repo.GetMod(x.Dependency);
                var mod = new ModRepoContent(x.Dependency, x.Dependency, col.GameId, repoContent.Value.GetVersionInfo());
                if (repoContent.Value.Dependencies != null)
                    mod.Dependencies = GetDependencyTree(repoContent, customRepos, content);
                return mod;
            }

            private static List<string> GetDependencyTree(KeyValuePair<string, SixRepoModDto> repoContent,
                IReadOnlyCollection<CustomRepo> customRepos,
                IReadOnlyCollection<NetworkContent> content) {
                var dependencies = new List<string>();
                var name = repoContent.Key.ToLower();
                // TODO: Would be better to build the dependency tree from actual objects instead of strings??
                BuildDependencyTree(dependencies, repoContent, customRepos, content);
                dependencies.Remove(name); // we dont want ourselves to be a dep of ourselves
                return dependencies;
            }

            private static void BuildDependencyTree(List<string> dependencies,
                KeyValuePair<string, SixRepoModDto> repoContent,
                IReadOnlyCollection<CustomRepo> customRepos, IReadOnlyCollection<NetworkContent> content) {
                var name = repoContent.Key.ToLower();
                if (dependencies.Contains(name))
                    return;
                dependencies.Add(name);
                if (repoContent.Value.Dependencies == null)
                    return;

                foreach (var d in repoContent.Value.Dependencies) {
                    var n = d.ToLower();
                    var repo = customRepos.FirstOrDefault(r => r.HasMod(d));
                    if (repo == null) {
                        var nc =
                            content.FirstOrDefault(
                                x => x.PackageName.Equals(d, StringComparison.InvariantCultureIgnoreCase));
                        if (nc != null) {
                            var deps =
                                nc.GetRelatedContent()
                                    .Select(x => x.Content)
                                    .OfType<IHavePackageName>()
                                    .Select(x => x.PackageName)
                                    .Where(x => !dependencies.ContainsIgnoreCase(x))
                                    .ToArray();
                            // TODO: this does not take care of dependencies that actually exist then on the custom repo, and might have different deps setup than the official network counter parts..
                            // But the use case is very limited..
                            dependencies.AddRange(deps);
                        } else
                            dependencies.Add(n);
                    } else
                        BuildDependencyTree(dependencies, repo.GetMod(d), customRepos, content);
                }

                dependencies.Remove(name);
                dependencies.Add(name);
            }

            private static Content ConvertToContentOrLocal(CollectionVersionDependencyModel x, IHaveGameId col,
                IEnumerable<NetworkContent> content) => (Content) content.FirstOrDefault(
                    cnt =>
                        cnt.PackageName.Equals(x.Dependency,
                            StringComparison.CurrentCultureIgnoreCase))
                                                        ??
                                                        new ModLocalContent(x.Dependency, x.Dependency.ToLower(),
                                                            col.GameId,
                                                            new BasicInstallInfo());

            public async Task<List<CollectionModelWithLatestVersion>> DownloadCollections(
                IEnumerable<Tuple<Guid, List<Guid>>> gamesWithCollections) {
                var list = new List<CollectionModelWithLatestVersion>();
                foreach (var g in gamesWithCollections)
                    list.AddRange(await RetrieveCollections(g.Item1, g.Item2).ConfigureAwait(false));
                return list;
            }

            private async Task<List<CollectionModelWithLatestVersion>> RetrieveCollections(Guid gameId,
                IEnumerable<Guid> colIds) {
                var token = await GetToken().ConfigureAwait(false);
                return
                    await
                        Tools.Transfer.GetJson<List<CollectionModelWithLatestVersion>>(
                            new Uri(CommonUrls.ApiUrl + "/api/collections?gameId=" + gameId +
                                    string.Join("", colIds.Select(x => "&ids=" + x))), token).ConfigureAwait(false);
            }

            private async Task<string> GetToken() {
                var sContext = _locator.GetSettingsContext();
                return (await sContext.GetSettings().ConfigureAwait(false)).Secure.Login?.Authentication.AccessToken;
            }

            public async Task<IReadOnlyCollection<SubscribedCollection>> GetCollections(Guid gameId,
                IReadOnlyCollection<Guid> collectionIds, IReadOnlyCollection<NetworkContent> content) {
                var contents =
                    await DownloadCollections(new[] {Tuple.Create(gameId, collectionIds.ToList())})
                        .ConfigureAwait(false);
                if (contents.Count < collectionIds.Count)
                    throw new NotFoundException("Could not find all requested collections");
                var collections = new List<SubscribedCollection>();
                foreach (var c in contents) {
                    var col = c.MapTo<SubscribedCollection>();
                    await HandleContent(content, col, c).ConfigureAwait(false);
                    col.UpdateState();
                    collections.Add(col);
                }
                return collections;
            }
        }
    }
}