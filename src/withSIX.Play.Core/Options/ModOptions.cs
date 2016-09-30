// <copyright company="SIX Networks GmbH" file="ModOptions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ReactiveUI;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Games.Legacy.Repo;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Core.Options
{
    [DataContract(Name = "ModOptions", Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class ModOptions : OptionBase
    {
        [DataMember] List<Guid> _acceptedLicenseUUIDs = new List<Guid>();
        [DataMember] bool? _autoProcessModApps;
        [DataMember] long _collectionId;
        [DataMember] List<CustomCollection> _customModSets = new List<CustomCollection>();
        [DataMember] List<FavoriteMod> _favoriteMods = new List<FavoriteMod>();
        [DataMember] List<FavoriteCollection> _favorites = new List<FavoriteCollection>();
        [DataMember] List<LocalModsContainer> _localMods = new List<LocalModsContainer>();
        [DataMember] ReactiveList<RecentCollection> _recentModSets = new ReactiveList<RecentCollection>();
        [DataMember] Dictionary<string, SixRepo> _repositories = new Dictionary<string, SixRepo>();
        [DataMember] List<SubscribedCollection> _subscribedModSets = new List<SubscribedCollection>();
        [DataMember] Dictionary<string, string> _userConfigChecksums = new Dictionary<string, string>();
        [DataMember] ViewType _viewType;
        public List<FavoriteCollection> FavoriteCollections
        {
            get { return _favorites ?? (_favorites = new List<FavoriteCollection>()); }
            set { _favorites = value; }
        }
        public Dictionary<string, SixRepo> Repositories
        {
            get { return (_repositories ?? (_repositories = new Dictionary<string, SixRepo>())); }
            set { _repositories = value; }
        }
        public Dictionary<string, string> UserConfigChecksums
        {
            get { return (_userConfigChecksums ?? (_userConfigChecksums = new Dictionary<string, string>())); }
            set { _userConfigChecksums = value; }
        }
        public List<CustomCollection> CustomCollections
        {
            get { return _customModSets ?? (_customModSets = new List<CustomCollection>()); }
            set { _customModSets = value; }
        }
        public List<SubscribedCollection> SubscribedCollections
        {
            get { return _subscribedModSets ?? (_subscribedModSets = new List<SubscribedCollection>()); }
            set { _subscribedModSets = value; }
        }
        public List<Guid> AcceptedLicenseUUIDs
        {
            get { return _acceptedLicenseUUIDs ?? (_acceptedLicenseUUIDs = new List<Guid>()); }
            set { _acceptedLicenseUUIDs = value; }
        }
        public List<FavoriteMod> FavoriteMods
        {
            get { return _favoriteMods ?? (_favoriteMods = new List<FavoriteMod>()); }
            set { _favoriteMods = value; }
        }
        public ReactiveList<RecentCollection> RecentCollections
        {
            get { return _recentModSets ?? (_recentModSets = new ReactiveList<RecentCollection>()); }
            set { _recentModSets = value; }
        }
        public List<LocalModsContainer> LocalMods
        {
            get { return _localMods ?? (_localMods = new List<LocalModsContainer>()); }
            set { _localMods = value; }
        }
        public long CollectionId
        {
            get { return _collectionId; }
            set { _collectionId = value; }
        }
        public bool AutoProcessModApps
        {
            get { return _autoProcessModApps.GetValueOrDefault(true); }
            set { _autoProcessModApps = value; }
        }
        public ViewType ViewType
        {
            get { return _viewType; }
            set { _viewType = value; }
        }

        public bool IsFavorite(Collection collection) => FavoriteCollections.Any(f => f.Matches(collection));

        public bool IsFavorite(IMod mod) => FavoriteMods.Any(f => f.Matches(mod));

        public void AddFavorite(Collection collection) {
            lock (FavoriteCollections) {
                if (FavoriteCollections.Any(f => f.Matches(collection)))
                    return;
                FavoriteCollections.Add(new FavoriteCollection(collection));
            }
            SaveSettings();
        }

        public void AddFavorite(IMod mod) {
            lock (FavoriteMods) {
                if (FavoriteMods.Any(f => f.Matches(mod)))
                    return;
                FavoriteMods.Add(new FavoriteMod(mod));
            }
            SaveSettings();
        }

        public void RemoveFavorite(Collection collection) {
            lock (FavoriteCollections) {
                var favorite = FavoriteCollections.FirstOrDefault(f => f.Matches(collection));
                if (favorite == null)
                    return;
                FavoriteCollections.Remove(favorite);
            }
            SaveSettings();
        }

        public void RemoveFavorite(IMod mod) {
            lock (FavoriteMods) {
                var favorite = FavoriteMods.FirstOrDefault(f => f.Matches(mod));
                if (favorite == null)
                    return;
                FavoriteMods.Remove(favorite);
            }
            SaveSettings();
        }

        public void AddAcceptedLicenses(IEnumerable<IMod> mods) {
            AcceptedLicenseUUIDs.AddRangeLocked(mods.Select(x => x.Id));
            SaveSettings();
        }

        public void AddRecent(Collection collection) {
            var recentModSet = new RecentCollection(collection);
            RecentCollections.AddLocked(recentModSet);
            recentModSet.Collection = collection;
            collection.RefreshLastJoinedOn();
            SaveSettings();
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            _recentModSets?.RemoveAll(_recentModSets.Where(x => x.Id == Guid.Empty).ToArray());
        }
    }
}