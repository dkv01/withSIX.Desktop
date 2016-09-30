// <copyright company="SIX Networks GmbH" file="RecentCollection.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Core.Options.Entries
{
    [DataContract(Name = "RecentModSet",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class RecentCollection : PropertyChangedBase
    {
        [DataMember] readonly string _Name;
        [DataMember] readonly DateTime _on;
        Collection _collection;
        [DataMember] Guid _id;
        [Obsolete, DataMember] string _Uuid;

        public RecentCollection(Collection collection) {
            _collection = collection;
            _on = Tools.Generic.GetCurrentUtcDateTime;
            _id = collection.Id;
            _Name = collection.Name;
        }

        public Guid Id => _id;
        public string Name => _Name;
        public DateTime On => _on;
        public Collection Collection
        {
            get { return _collection; }
            set { SetProperty(ref _collection, value); }
        }

        public bool Matches(Collection collection) => collection != null && collection.Id == _id;

        public string GetLaunchUrl() => $"pws://?mod_set={_id}";

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context) {
            if (_Uuid != null) {
                Guid.TryParse(_Uuid, out _id);
                _Uuid = null;
            }
        }
    }
}