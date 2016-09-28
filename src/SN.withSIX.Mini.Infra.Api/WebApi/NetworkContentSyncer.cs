// <copyright company="SIX Networks GmbH" file="NetworkContentSyncer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Social;
using SN.withSIX.Mini.Infra.Data.Services;
using SN.withSIX.Sync.Core;
using SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo;
using SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo.dtos;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;
using ContentGuidSpec = withSIX.Api.Models.Content.v3.ContentGuidSpec;

namespace SN.withSIX.Mini.Infra.Api.WebApi
{
    // TODO: optimize synchronizing collection with group/repo content dependencies.
    public class NetworkContentSyncer : IInfrastructureService, INetworkContentSyncer
    {
        private readonly CollectionSyncer _collectionSyncer;
        readonly IDbContextLocator _locator;

        public NetworkContentSyncer(IDbContextLocator locator) {
            _locator = locator;
            _collectionSyncer = new CollectionSyncer(locator, this);
        }

        public async Task SyncContent(IReadOnlyCollection<Game> games, ContentQuery filterFunc = null) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Syncing content for games: {string.Join(", ", games.Select(x => x.Id))}");
            foreach (var g in games)
                await ProcessGame(g, filterFunc).ConfigureAwait(false);
        }

        public Task SyncCollections(IReadOnlyCollection<SubscribedCollection> collections,
            bool countCheck = true)
            => _collectionSyncer.SyncCollections(collections, countCheck);

        public Task<IReadOnlyCollection<SubscribedCollection>> GetCollections(Guid gameId,
            IReadOnlyCollection<Guid> collectionIds)
            => _collectionSyncer.GetCollections(gameId, collectionIds);

        async Task<Dictionary<Guid, ModClientApiJsonV3WithGameId>> GetContentList(Guid gameId, ApiHashes hashes) {
            var r = await _locator.GetApiContext().GetMods(gameId, hashes.Mods).ConfigureAwait(false);
            return r.ToDictionary(x => x.Id, x => x);
        }

        async Task ProcessGame(Game game, ContentQuery filterFunc) {
            var invalidContent = game.Contents.Where(x => x.GameId == Guid.Empty).ToArray();
            if (invalidContent.Any())
                invalidContent.ForEach(x => x.FixGameId(game.Id));

            var stats = await GetHashStats(game).ConfigureAwait(false);
            if (!stats.ShouldSyncBecauseHashes && !stats.ShouldSyncBecauseVersion && filterFunc == null)
                return;
            var onlineContent = await GetContent(game, stats.Hashes).ConfigureAwait(false);
            ProcessContents(game, onlineContent, filterFunc);
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

        Task<ApiHashes> GetHashesV3(Guid gameId) => _locator.GetApiContext().GetHashes(gameId);

        private async Task<Dictionary<Guid, ModClientApiJsonV3WithGameId>> GetContent(Game game, ApiHashes latestHashes) {
            var compatGameIds = game.GetCompatibleGameIds();
            var ctx = _locator.GetGameContext();
            await ctx.Load(compatGameIds).ConfigureAwait(false);

            // TODO: Only process and keep content that we actually are looking for (1. Installed content, 2. Desired content)
            var mods = new Dictionary<Guid, ModClientApiJsonV3WithGameId>();
            foreach (var c in compatGameIds) {
                var cMods = await GetContentList(c, latestHashes).ConfigureAwait(false);
                foreach (var m in cMods)
                    m.Value.GameId = c;
                mods.AddRange(cMods);
            }

            return mods;
        }

        static void ProcessContents(Game game, IDictionary<Guid, ModClientApiJsonV3WithGameId> onlineContent,
            ContentQuery filterFunc) {
            // TODO: If we timestamp the DTO's, and save the timestamp also in our database,
            // then we can simply update data only when it has actually changed and speed things up.
            // The only thing to remember is when there are schema changes / new fields etc, either all timestamps need updating
            // or the syncer needs to take it into account..
            var mapping = new Dictionary<ModClientApiJsonV3WithGameId, ModNetworkContent>();
            UpdateContents(game, onlineContent, mapping, filterFunc);
            HandleDependencies(game, mapping);
            if (filterFunc == null)
                game.ProcessLocalContent();
        }

        static void UpdateContents(Game game, IDictionary<Guid, ModClientApiJsonV3WithGameId> onlineContent,
            IDictionary<ModClientApiJsonV3WithGameId, ModNetworkContent> content, ContentQuery filterFunc = null) {
            var currentContent = game.NetworkContent.OfType<ModNetworkContent>().ToDictionary(x => x.Id, x => x);
            var desiredModsList = GetDesiredModList(game, onlineContent, filterFunc, currentContent);
            var mapping = desiredModsList.Keys
                .Where(onlineContent.ContainsKey)
                .Select(
                    x =>
                        new {
                            DTO = onlineContent[x],
                            Existing = currentContent.ContainsKey(x) ? currentContent[x] : null
                        });

            foreach (var c in mapping) {
                var dto = c.DTO;
                var theGame = SetupGameStuff.GameSpecs.Select(x => x.Value).FindOrThrow(c.DTO.GameId);
                if (c.Existing == null) {
                    var nc = dto.MapTo<ModNetworkContent>();
                    content[dto] = nc;
                    game.Contents.Add(nc);
                } else {
                    c.Existing.UpdateVersionInfo(dto.GetVersion(), dto.UpdatedVersion);
                    dto.MapTo(c.Existing);
                    content[dto] = c.Existing;
                }
                if (c.DTO.GameId != game.Id)
                    content[dto].HandleOriginalGame(c.DTO.GameId, theGame.Slug, game.Id);
            }
        }

        private static Dictionary<Guid, ModClientApiJsonV3WithGameId> GetDesiredModList(Game game,
            IDictionary<Guid, ModClientApiJsonV3WithGameId> onlineContent, ContentQuery filterFunc,
            Dictionary<Guid, ModNetworkContent> currentContent) {
            if (filterFunc != null)
                return GetTheDesiredMods(game, filterFunc, onlineContent);

            var localMods =
                game.LocalContent.OfType<ModLocalContent>()
                    .Select(
                        x =>
                            onlineContent.Values.FirstOrDefault(
                                c => c.PackageName.Equals(x.PackageName, StringComparison.CurrentCultureIgnoreCase)))
                    .Where(x => x != null);
            var ids = currentContent.Keys.Concat(localMods.Select(x => x.Id)).Distinct();
            return GetTheDesiredMods(game, new ContentQuery {Ids = ids.ToList()}, onlineContent);
        }

        private static Dictionary<Guid, ModClientApiJsonV3WithGameId> GetTheDesiredMods(Game game,
            ContentQuery filterFunc,
            IDictionary<Guid, ModClientApiJsonV3WithGameId> onlineContent) {
            var desired = onlineContent.Where(x => filterFunc.IsMatch(x.Value));
            var dependencyChain = new Dictionary<Guid, ModClientApiJsonV3WithGameId>();
            GetRelatedContent(game, desired.Select(x => x.Value), dependencyChain, onlineContent);
            return dependencyChain;
        }

        private static void GetRelatedContent(Game game, IEnumerable<ModClientApiJsonV3WithGameId> desired,
            IDictionary<Guid, ModClientApiJsonV3WithGameId> dependencyChain,
            IDictionary<Guid, ModClientApiJsonV3WithGameId> onlineContent) {
            foreach (var c in desired)
                GetRelatedContent(game, c, dependencyChain, onlineContent);
        }

        private static void GetRelatedContent(Game game, ModClientApiJsonV3WithGameId c,
            IDictionary<Guid, ModClientApiJsonV3WithGameId> dependencyChain,
            IDictionary<Guid, ModClientApiJsonV3WithGameId> onlineContent) {
            // A dictionary would not retain order, however we dont need to retain order currently
            if (dependencyChain.ContainsKey(c.Id))
                return;
            dependencyChain.Add(c.Id, c);
            var defaultTags = new List<string>();

            if (c.GameId != game.Id) {
                // Make a copy of the DTO, and clone the list, because otherwise the changes might bleed through to other games, or cached re-usages
                c = c.MapTo<ModClientApiJsonV3WithGameId>();
                c.Dependencies = c.Dependencies.ToList();
                var cMods = game.GetCompatibilityMods(c.PackageName, c.Tags ?? defaultTags);
                foreach (var m in cMods) {
                    var theM =
                        onlineContent.Values.FirstOrDefault(
                            x => x.PackageName.Equals(m, StringComparison.CurrentCultureIgnoreCase));
                    if (theM != null && c.Dependencies.All(x => x.Id != theM.Id))
                        c.Dependencies.Add(new ContentGuidSpec {Id = theM.Id});
                }
            }
            GetRelatedContent(game, c.Dependencies.Select(x => onlineContent[x.Id]), dependencyChain, onlineContent);
            dependencyChain.Remove(c.Id);
            dependencyChain.Add(c.Id, c);
        }

        static void HandleDependencies(Game game, Dictionary<ModClientApiJsonV3WithGameId, ModNetworkContent> content) {
            var c = content.Values.ToDictionary(x => x.Id, x => x);
            foreach (var nc in content)
                HandleDependencies(nc, c);
        }

        // TODO: catch frigging circular reference mayhem!
        // http://stackoverflow.com/questions/16472958/json-net-silently-ignores-circular-references-and-sets-arbitrary-links-in-the-ch
        // http://stackoverflow.com/questions/21686499/how-to-restore-circular-references-e-g-id-from-json-net-serialized-json
        static void HandleDependencies(KeyValuePair<ModClientApiJsonV3WithGameId, ModNetworkContent> nc,
            IDictionary<Guid, ModNetworkContent> networkContent) {
            var foundDeps = nc.Key.Dependencies.Select(d => networkContent[d.Id])
                .Select(x => new NetworkContentSpec(x));
            nc.Value.ReplaceDependencies(foundDeps);
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
            private readonly NetworkContentSyncer _networkContentSyncer;

            public CollectionSyncer(IDbContextLocator locator, NetworkContentSyncer networkContentSyncer) {
                _locator = locator;
                _networkContentSyncer = networkContentSyncer;
            }

            public async Task SyncCollections(IReadOnlyCollection<SubscribedCollection> collections,
                bool countCheck = true) {
                var contents =
                    await
                        DownloadCollections(
                            collections.GroupBy(x => x.GameId)
                                .Select(x => new Tuple<Guid, List<Guid>>(x.Key, x.Select(t => t.Id).ToList())))
                            .ConfigureAwait(false);

                if (countCheck && contents.Count < collections.Count)
                    throw new NotFoundException("Could not find all requested collections");

                foreach (var c in contents.Select(x => new {x, Col = collections.FindOrThrow(x.Id)}))
                    await MapExistingCollection(c.x, c.Col).ConfigureAwait(false);
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

            private async Task HandleContent(NetworkCollection col,
                CollectionModelWithLatestVersion c, List<NetworkCollection> collections = null) {
                if (collections == null)
                    collections = new List<NetworkCollection> {col};
                var collectionSpecs = await ProcessEmbeddedCollections(c, collections).ConfigureAwait(false);
                var modSpecs =
                    await
                        ProcessMods(col, c,
                            await _locator.GetGameContext().Games.FindOrThrowAsync(col.GameId).ConfigureAwait(false))
                            .ConfigureAwait(false);
                col.ReplaceContent(modSpecs.Concat(collectionSpecs));
            }

            private async Task<IReadOnlyCollection<ContentSpec>> ProcessMods(NetworkCollection col,
                CollectionModelWithLatestVersion c, Game game) {
                var customRepos = await GetRepositories(col).ConfigureAwait(false);
                var groupContent = await GetGroupContent(col).ConfigureAwait(false);
                var deps = c.LatestVersion
                    .Dependencies
                    .Where(x => x.DependencyType == DependencyType.Package).ToArray();
                var found = new List<Tuple<Content, string, CollectionVersionDependencyModel>>();
                foreach (var d in deps) {
                    var content = await ConvertToGroupOrRepoContent(d, col, customRepos, groupContent, game).ConfigureAwait(false);
                    if (content == null)
                        continue;
                    var t = Tuple.Create(content, d.Constraint, d);
                    found.Add(t);
                }
                var todo = deps.Except(found.Select(x => x.Item3)).ToArray();
                if (todo.Any())
                    await SynchronizeContent(game, todo.Select(x => x.Dependency)).ConfigureAwait(false);

                return
                    todo.Select(x => new ContentSpec(ConvertToContentOrLocal(x, col, game), x.Constraint))
                        .Where(x => x.Content != null)
                        .Concat(found.Select(x => new ContentSpec(x.Item1, x.Item2)))
                        .ToArray();
            }

            private Task SynchronizeContent(Game game, IEnumerable<string> packageNames)
                => _networkContentSyncer.SyncContent(
                    new[] {game}, new ContentQuery {PackageNames = packageNames.ToList()});

            private async Task<IEnumerable<ContentSpec>> ProcessEmbeddedCollections(
                CollectionModelWithLatestVersion c, List<NetworkCollection> collections) {
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
                        await GetContentSpec(collections, todoCols, ec, cols).ConfigureAwait(false);
                    buildCollections.Add(contentSpec);
                }
                return buildCollections;
            }

            private async Task<ContentSpec> GetContentSpec(List<NetworkCollection> collections,
                CollectionVersionDependencyModel[] todoCols,
                CollectionVersionDependencyModel ec, IEnumerable<CollectionModelWithLatestVersion> cols) {
                if (todoCols.Contains(ec)) {
                    return
                        await ProcessEmbeddedCollection(cols, ec, collections).ConfigureAwait(false);
                }
                return new ContentSpec(collections.First(co => co.Id == ec.CollectionDependencyId.Value),
                    ec.Constraint);
            }

            private async Task<ContentSpec> ProcessEmbeddedCollection(
                IEnumerable<CollectionModelWithLatestVersion> cols, CollectionVersionDependencyModel ec,
                List<NetworkCollection> collections) {
                var rc = cols.First(c2 => c2.Id == ec.CollectionDependencyId.Value);
                var conv = await MapCollection(rc, collections).ConfigureAwait(false);
                collections.Add(conv);
                return new ContentSpec(conv, ec.Constraint);
            }

            private async Task MapExistingCollection(CollectionModelWithLatestVersion rc, SubscribedCollection collection) {
                var userId = await GetUserId().ConfigureAwait(false);
                var conv = rc.MapTo(collection, opts => opts.Items["user-id"] = userId);
                // TODO: Allow parent repos to be used for children etc? :-)
                await HandleContent(conv, rc).ConfigureAwait(false);
                conv.UpdateState();
            }

            private async Task<SubscribedCollection> MapCollection(CollectionModelWithLatestVersion rc, List<NetworkCollection> collections = null) {
                var userId = await GetUserId().ConfigureAwait(false);
                var conv = rc.MapTo<SubscribedCollection>(opts => opts.Items["user-id"] = userId);
                // TODO: Allow parent repos to be used for children etc? :-)
                await HandleContent(conv, rc, collections).ConfigureAwait(false);
                conv.UpdateState();
                return conv;
            }

            private async Task<Guid> GetUserId() {
                var settings = await _locator.GetSettingsContext().GetSettings().ConfigureAwait(false);
                var userId = settings.Secure.Login.IsLoggedIn ? settings.Secure.Login.Account.Id : Guid.Empty;
                return userId;
            }

            private async Task<Content> ConvertToGroupOrRepoContent(CollectionVersionDependencyModel x, Collection col,
                IReadOnlyCollection<CustomRepo> customRepos, IEnumerable<GroupContent> groupContent, Game game) {
                var gc =
                    groupContent.FirstOrDefault(
                        c => c.PackageName.Equals(x.Dependency, StringComparison.CurrentCultureIgnoreCase));
                if (gc != null)
                    // TODO: dependencies etc
                    return new ModNetworkGroupContent(gc.Id, gc.PackageName, gc.GameId, gc.Version);
                return await HandleRepoContent(x, col, customRepos, game).ConfigureAwait(false);
            }

            private async Task<Content> HandleRepoContent(CollectionVersionDependencyModel x, Collection col,
                IReadOnlyCollection<CustomRepo> customRepos, Game game) {
                var repo = customRepos.FirstOrDefault(r => r.HasMod(x.Dependency));
                if (repo == null)
                    return null;
                var repoContent = repo.GetMod(x.Dependency);
                var mod = new ModRepoContent(x.Dependency, col.GameId, repoContent.Value.GetVersionInfo());
                if (repoContent.Value.Dependencies != null)
                    mod.Dependencies = await GetDependencyTree(repoContent, customRepos, game).ConfigureAwait(false);
                return mod;
            }

            private async Task<List<string>> GetDependencyTree(KeyValuePair<string, SixRepoModDto> repoContent,
                IReadOnlyCollection<CustomRepo> customRepos, Game game) {
                var dependencies = new List<string>();
                var name = repoContent.Key.ToLower();
                // TODO: Would be better to build the dependency tree from actual objects instead of strings??
                await BuildDependencyTree(game, dependencies, repoContent, customRepos).ConfigureAwait(false);
                dependencies.Remove(name); // we dont want ourselves to be a dep of ourselves
                return dependencies;
            }

            private async Task BuildDependencyTree(Game game, List<string> dependencies,
                KeyValuePair<string, SixRepoModDto> repoContent,
                IReadOnlyCollection<CustomRepo> customRepos) {
                var name = repoContent.Key.ToLower();
                if (dependencies.Contains(name))
                    return;
                dependencies.Add(name);
                if (repoContent.Value.Dependencies == null)
                    return;

                var theC = repoContent.Value.Dependencies.ToDictionary(x => x.ToLower(),
                    x => customRepos.FirstOrDefault(r => r.HasMod(x)));

                var notFound = theC.Where(x => x.Value == null).Select(x => x.Key).ToList();
                if (notFound.Any())
                    await SynchronizeContent(game, notFound).ConfigureAwait(false);

                foreach (var d in repoContent.Value.Dependencies) {
                    var n = d.ToLower();
                    var repo = customRepos.FirstOrDefault(r => r.HasMod(d));
                    if (repo == null) {
                        var nc =
                            game.NetworkContent.FirstOrDefault(
                                x => x.PackageName.Equals(d, StringComparison.OrdinalIgnoreCase));
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
                        await BuildDependencyTree(game, dependencies, repo.GetMod(d), customRepos).ConfigureAwait(false);
                }

                dependencies.Remove(name);
                dependencies.Add(name);
            }

            private static Content ConvertToContentOrLocal(CollectionVersionDependencyModel x, IHaveGameId col,
                Game game) => (Content) game.NetworkContent.FirstOrDefault(
                    cnt =>
                        cnt.PackageName.Equals(x.Dependency,
                            StringComparison.CurrentCultureIgnoreCase))
                              ??
                              new ModLocalContent(x.Dependency.ToLower(),
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
                                    string.Join("", colIds.Select(x => "&ids=" + x))), token: token).ConfigureAwait(false);
            }

            private async Task<string> GetToken() {
                var sContext = _locator.GetSettingsContext();
                return (await sContext.GetSettings().ConfigureAwait(false)).Secure.Login?.Authentication.AccessToken;
            }

            public async Task<IReadOnlyCollection<SubscribedCollection>> GetCollections(Guid gameId,
                IReadOnlyCollection<Guid> collectionIds) {
                var contents =
                    await DownloadCollections(new[] {Tuple.Create(gameId, collectionIds.ToList())})
                        .ConfigureAwait(false);
                if (contents.Count < collectionIds.Count)
                    throw new NotFoundException("Could not find all requested collections");
                var collections = new List<SubscribedCollection>();
                foreach (var c in contents)
                    collections.Add(await MapCollection(c).ConfigureAwait(false));
                return collections;
            }
        }
    }
}