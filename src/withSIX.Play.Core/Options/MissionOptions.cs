// <copyright company="SIX Networks GmbH" file="MissionOptions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ReactiveUI;
using withSIX.Core.Extensions;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Missions;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Core.Options
{
    [DataContract(Name = "MissionOptions",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Options")]
    public class MissionOptions : OptionBase
    {
        static Type[] _knownTypes;
        [DataMember] List<FavoriteMission> _favorites = new List<FavoriteMission>();
        [DataMember] List<LocalMissionsContainer> _localMissions = new List<LocalMissionsContainer>();
        [DataMember] ReactiveList<RecentMission> _recentMissions = new ReactiveList<RecentMission>();
        [DataMember] ViewType _viewType;
        public ViewType ViewType
        {
            get { return _viewType; }
            set { _viewType = value; }
        }
        public List<FavoriteMission> Favorites
        {
            get { return _favorites ?? (_favorites = new List<FavoriteMission>()); }
            set { _favorites = value; }
        }
        public List<LocalMissionsContainer> LocalMissions
        {
            get { return _localMissions ?? (_localMissions = new List<LocalMissionsContainer>()); }
            set { _localMissions = value; }
        }
        public ReactiveList<RecentMission> RecentMissions
        {
            get { return _recentMissions ?? (_recentMissions = new ReactiveList<RecentMission>()); }
            set { _recentMissions = value; }
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            _recentMissions?.RemoveAll(_recentMissions.Where(x => x.Key == null).ToArray());
            _favorites?.RemoveAll(x => x.Key == null);
        }

        public void AddFavorite(MissionBase mission) {
            lock (Favorites) {
                if (Favorites.Any(f => f.Matches(mission)))
                    return;
                Favorites.Add(new FavoriteMission(mission));
            }
            SaveSettings();
        }

        public void RemoveFavorite(MissionBase mission) {
            lock (Favorites) {
                var favorite = Favorites.FirstOrDefault(f => f.Matches(mission));
                if (favorite == null)
                    return;
                Favorites.Remove(favorite);
            }
            SaveSettings();
        }

        public bool IsFavorite(MissionBase mission) => Favorites.Any(f => f.Matches(mission));

        public void AddRecent(MissionBase mission) {
            var recentModSet = new RecentMission(mission);
            RecentMissions.AddLocked(recentModSet);
            mission.RefreshLastJoinedOn();
            SaveSettings();
        }
    }
}