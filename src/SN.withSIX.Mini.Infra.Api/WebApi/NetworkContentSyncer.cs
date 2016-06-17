// <copyright company="SIX Networks GmbH" file="NetworkContentSyncer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Api.Models.Collections;
using SN.withSIX.Api.Models.Exceptions;
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

namespace SN.withSIX.Mini.Infra.Api.WebApi
{
    public class NetworkContentSyncer : IInfrastructureService, INetworkContentSyncer
    {
        readonly IDbContextLocator _locator;

        public NetworkContentSyncer(IDbContextLocator locator) {
            _locator = locator;
        }

        public Task<ApiHashes> GetHashes()
            => Tools.Transfer.GetJson<ApiHashes>(new Uri("http://api-cdn.withsix.com/api/v2/hashes.json.gz"));

        public async Task SyncContent(IReadOnlyCollection<Game> games, ApiHashes hashes) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Syncing content for games: {string.Join(", ", games.Select(x => x.Id))}");
            var allNetworkContent =
                await
                    DownloadContentLists(games.SelectMany(x => x.GetCompatibleGameIds()), hashes).ConfigureAwait(false);
            foreach (var g in games)
                await ProcessGame(g, allNetworkContent).ConfigureAwait(false);
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

            foreach (var c in contents) {
                var col = collections.FindOrThrow(c.Id);
                c.MapTo(col);
                HandleContent(content, col, c, await GetRepositories(col).ConfigureAwait(false),
                    await GetGroupContent(col).ConfigureAwait(false));
                col.UpdateState();
            }
        }

        public async Task<IReadOnlyCollection<SubscribedCollection>> GetCollections(Guid gameId,
            IReadOnlyCollection<Guid> collectionIds, IReadOnlyCollection<NetworkContent> content) {
            var contents =
                await
                    DownloadCollections(new[] {Tuple.Create(gameId, collectionIds.ToList())})
                        .ConfigureAwait(false);
            if (contents.Count < collectionIds.Count)
                throw new NotFoundException("Could not find all requested collections");
            var collections = new List<SubscribedCollection>();
            foreach (var c in contents) {
                var col = c.MapTo<SubscribedCollection>();
                HandleContent(content, col, c, await GetRepositories(col).ConfigureAwait(false),
                    await GetGroupContent(col).ConfigureAwait(false));
                col.UpdateState();
                collections.Add(col);
            }
            return collections;
        }

        private async Task<IReadOnlyCollection<GroupContent>> GetGroupContent(SubscribedCollection col) {
            if (!col.GroupId.HasValue)
                return new GroupContent[0];
            var group = new Group(col.GroupId.Value, "UNKNOWN");
            await group.Load(await GetToken().ConfigureAwait(false)).ConfigureAwait(false);
            return group.Content;
        }

        static async Task<CustomRepo[]> GetRepositories(IHaveRepositories col) {
            var repositories = col.Repositories.Select(r => new CustomRepo(CustomRepo.GetRepoUri(new Uri(r)))).ToArray();
            foreach (var r in repositories) {
                try {
                    await r.Load(SyncEvilGlobal.StringDownloader, r.Uri).ConfigureAwait(false);
                } catch (Exception ex) {
                    MainLog.Logger.FormattedWarnException(ex, "Error while processing repo");
                }
            }

            return repositories.Where(x => x.Loaded).ToArray();
        }

        async Task<List<ModDto>> DownloadContentLists(IEnumerable<Guid> gameIds, ApiHashes hashes) {
            var mods =
                await
                    Tools.Transfer.GetJson<List<ModDto>>(
                        new Uri("http://api-cdn.withsix.com/api/v2/mods.json.gz?v=" + hashes.Mods))
                        .ConfigureAwait(false);
            return mods.Where(x => gameIds.Contains(x.GameId)).ToList();
        }

        async Task ProcessGame(Game game, IEnumerable<ModDto> allContent) {
            var compatGameIds = game.GetCompatibleGameIds();
            var ctx = _locator.GetGameContext();
            await ctx.Load(compatGameIds).ConfigureAwait(false);
            var gameContent = allContent.Where(x => compatGameIds.Contains(x.GameId)).ToArray();
            //var allGames = ctx.Games.Where(x => compatGameIds.Contains(x.Id)).ToArray();
            ProcessContents(game, gameContent);
            ProcessLocalContent(game);
            game.RefreshCollections();
        }

        static void ProcessContents(Game game, IEnumerable<ModDto> contents) {
            // TODO: If we timestamp the DTO's, and save the timestamp also in our database,
            // then we can simply update data only when it has actually changed and speed things up.
            // The only thing to remember is when there are schema changes / new fields etc, either all timestamps need updating
            // or the syncer needs to take it into account..
            var mapping = new Dictionary<ModDto, ModNetworkContent>();
            UpdateContents(game, contents, mapping);
            HandleDependencies(game, mapping);
        }

        static void UpdateContents(Game game, IEnumerable<ModDto> contents,
            IDictionary<ModDto, ModNetworkContent> content) {
            var newContent = new List<ModNetworkContent>();
            var emptyTags = new List<string>();
            foreach (
                var c in
                    contents.Select(x => new {DTO = x, Existing = game.NetworkContent.OfType<ModNetworkContent>().Find(x.Id)})
                        .OrderByDescending(x => x.DTO.GameId == game.Id)) {
                var theDto = c.DTO;
                var theGame = SetupGameStuff.GameSpecs.Select(x => x.Value).FindOrThrow(c.DTO.GameId);
                if (c.DTO.GameId != game.Id) {
                    // Make a copy of the DTO, and clone the list, because otherwise the changes will bleed through to other games...
                    theDto = c.DTO.MapTo<ModDto>();
                    theDto.Dependencies = theDto.Dependencies.ToList();
                    theDto.Dependencies.AddWhenMissing(game.GetCompatibilityMods(theDto.PackageName,
                        theDto.Tags ?? emptyTags));
                }
                if (c.Existing == null) {
                    var nc = theDto.MapTo<ModNetworkContent>();
                    newContent.Add(nc);
                    content[theDto] = nc;
                } else {
                    c.Existing.UpdateVersionInfo(theDto.GetVersion(), theDto.UpdatedVersion);
                    theDto.MapTo(c.Existing);
                    content[theDto] = c.Existing;
                }
                if (c.DTO.GameId != game.Id) {
                    content[theDto].OriginalGameId = c.DTO.GameId;
                    content[theDto].OriginalGameSlug = theGame.Slug;
                    content[theDto].GameId = game.Id;
                }
            }
            game.Contents.AddRange(newContent);
        }

        static void HandleDependencies(Game game, Dictionary<ModDto, ModNetworkContent> content) {
            foreach (var nc in content)
                HandleDependencies(nc, game.NetworkContent.OfType<ModNetworkContent>());
        }

        // TODO: catch frigging circular reference mayhem!
        // http://stackoverflow.com/questions/16472958/json-net-silently-ignores-circular-references-and-sets-arbitrary-links-in-the-ch
        // http://stackoverflow.com/questions/21686499/how-to-restore-circular-references-e-g-id-from-json-net-serialized-json
        static void HandleDependencies(KeyValuePair<ModDto, ModNetworkContent> nc,
            IEnumerable<ModNetworkContent> networkContent) {
            nc.Value.ReplaceDependencies(
                nc.Key.Dependencies.Select(
                    d =>
                        networkContent.FirstOrDefault(
                            x => x.PackageName.Equals(d, StringComparison.CurrentCultureIgnoreCase)))
                    // TODO: Find out why we would have nulls..
                    .Where(x => x != null)
                    .Select(x => new NetworkContentSpec(x)));
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

        static void HandleContent(IReadOnlyCollection<NetworkContent> content, Collection col,
            CollectionModelWithLatestVersion c, IReadOnlyCollection<CustomRepo> customRepos,
            IEnumerable<GroupContent> groupContent) {
            col.ReplaceContent(
                c.LatestVersion
                    .Dependencies
                    .Select(
                        x =>
                            new {
                                Content = ConvertToGroupOrRepoContent(x, col, customRepos, groupContent, content) ??
                                          ConvertToContentOrLocal(x, col, content), // temporary
                                x.Constraint
                            })
                    .Where(x => x.Content != null)
                    .Select(x => new ContentSpec(x.Content, x.Constraint)));
        }

        static Content ConvertToGroupOrRepoContent(CollectionVersionDependencyModel x, Collection col,
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

        static List<string> GetDependencyTree(KeyValuePair<string, SixRepoModDto> repoContent,
            IReadOnlyCollection<CustomRepo> customRepos,
            IReadOnlyCollection<NetworkContent> content) {
            var dependencies = new List<string>();
            var name = repoContent.Key.ToLower();
            // TODO: Would be better to build the dependency tree from actual objects instead of strings??
            BuildDependencyTree(dependencies, repoContent, customRepos, content);
            dependencies.Remove(name); // we dont want ourselves to be a dep of ourselves
            return dependencies;
        }

        static void BuildDependencyTree(List<string> dependencies, KeyValuePair<string, SixRepoModDto> repoContent,
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
                        content.FirstOrDefault(x => x.PackageName.Equals(d, StringComparison.InvariantCultureIgnoreCase));
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

        static Content ConvertToContentOrLocal(CollectionVersionDependencyModel x, IHaveGameId col,
            IEnumerable<NetworkContent> content) => (Content) content.FirstOrDefault(
                cnt =>
                    cnt.PackageName.Equals(x.Dependency,
                        StringComparison.CurrentCultureIgnoreCase))
                                                    ??
                                                    new ModLocalContent(x.Dependency, x.Dependency.ToLower(), col.GameId,
                                                        new BasicInstallInfo());

        async Task<List<CollectionModelWithLatestVersion>> DownloadCollections(
            IEnumerable<Tuple<Guid, List<Guid>>> gamesWithCollections) {
            var apiHost =
                //#if DEBUG
                //"https://auth.local.withsix.net";
                //#else
                "https://auth.withsix.com";
            //#endif
            var list = new List<CollectionModelWithLatestVersion>();
            foreach (var g in gamesWithCollections) {
                list.AddRange(await
                    Tools.Transfer.GetJson<List<CollectionModelWithLatestVersion>>(
                        new Uri(apiHost + "/api/collections?gameId=" + g.Item1 +
                                string.Join("", g.Item2.Select(x => "&ids=" + x))), await GetToken())
                        .ConfigureAwait(false));
            }
            return list;
        }

        async Task<string> GetToken() {
            var sContext = _locator.GetSettingsContext();
            return (await sContext.GetSettings().ConfigureAwait(false)).Secure.Login?.Authentication.AccessToken;
        }
    }

    public class CollectionModelWithLatestVersion : withSIX.Api.Models.Collections.CollectionModelWithLatestVersion
    {
        public Guid? GroupId { get; set; }
        public long Size { get; set; }
        public long SizePacked { get; set; }
        public string Author { get; set; }
        public int ModsCount { get; set; }
    }
}