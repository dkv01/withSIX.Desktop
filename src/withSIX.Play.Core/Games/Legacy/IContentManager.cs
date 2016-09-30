// <copyright company="SIX Networks GmbH" file="IContentManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NDepend.Path;
using ReactiveUI;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.Arma3;
using withSIX.ContentEngine.Core;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy.Events;
using withSIX.Play.Core.Games.Legacy.Missions;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Games.Legacy.Repo;
using withSIX.Play.Core.Games.Legacy.Servers;
using withSIX.Api.Models.Content.v2;

namespace withSIX.Play.Core.Games.Legacy
{
    public interface IContentManager
    {
        IServerList ServerList { get; }
        IContentEngine ContentEngine { get; }
        ReactiveList<Collection> Collections { get; }
        ReactiveList<Mod> Mods { get; }
        ReactiveList<Mission> Missions { get; }
        ReactiveList<LocalMissionsContainer> LocalMissionsContainers { get; }
        ReactiveList<LocalModsContainer> LocalModsContainers { get; }
        ReactiveList<SixRepo> CustomRepositories { get; }
        ReactiveList<CustomCollection> CustomCollections { get; }
        ReactiveList<SubscribedCollection> SubscribedCollections { get; }
        bool SyncManagerSynced { get; }
        Task Sync(ApiHashes hashes = null, bool suppressExceptionDialog = false);
        CustomCollection CreateAndSelectCustomModSet(IContent content = null);
        CustomCollection CreateAndSelectCustomModSet(IReadOnlyCollection<IContent> content);
        Task HandlePwsUrl(string pwsUrl);
        Task RefreshCollectionInfo(Collection collection, bool report = true);
        Task<IAbsoluteFilePath> CreateIcon(Collection collection);
        Task InitialServerSync(bool updateOnlyWhenActive = false);
        void UpdateCollectionStates();
        Task<SixRepo> GetRepo(Uri uri);
        string GetSuggestedCollectionName(IContent content = null);
        CustomCollection CreateCustomRepoServerModSet(SixRepoServer repoServer, string key, SixRepo repo);

        IEnumerable<Mod> FindOrCreateLocalMods(ISupportModding game, IEnumerable<string> mods,
            IReadOnlyCollection<Mod> inputMods = null);

        Mod FindMod(string mod, IReadOnlyCollection<Mod> inputMods = null);

        IEnumerable<Mod> GetDependencies(ISupportModding game, IReadOnlyCollection<Mod> mods,
            IReadOnlyCollection<Mod> inputMods = null);

        IEnumerable<Mod> GetMods(ISupportModding game, IEnumerable<string> mods,
            IReadOnlyCollection<Mod> inputMods = null);

        //IEnumerable<string> GetModsInclDependencies(IReadOnlyCollection<string> modList, IReadOnlyCollection<Mod> inputMods = null);
        IEnumerable<Mod> CompatibleMods(IEnumerable<Mod> inputMods, ISupportModding game);
        Task<List<MissionModel>> GetMyMissions(Game game);
        Task PublishMission(MissionBase missionBase, string missionName);
        void SelectMission(MissionBase mission);
        void RemoveCollection(CustomCollection collection);
        Collection CloneCollection(Collection current);
        CustomCollection CreateAndAddCustomModSet(Server server);

        CustomCollection CreateCustomCollection(ISupportModding game, IContent content = null,
            CustomCollection existingCol = null);

        MissionBase[] GetLocalMissions(string path = null);
        Task InitAsync(bool isInternetAvailable);
        void SelectCollection(Collection collection);
        Task Handle(ActiveGameChanged notification);
        Task Handle(SubGamesChanged notification);
        Task ProcessLegacyCustomCollection(CustomCollection customCollection, bool report);
    }
}