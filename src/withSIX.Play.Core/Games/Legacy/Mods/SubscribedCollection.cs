// <copyright company="SIX Networks GmbH" file="SubscribedCollection.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using withSIX.Api.Models;
using withSIX.Api.Models.Collections;
using withSIX.Play.Core.Connect;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    [DataContract(Name = "CollectionServer",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    
    public class CollectionServer
    {
        [DataMember]
        public ServerAddress Address { get; set; }
        [DataMember]
        public string Password { get; set; }
    }

    [DataContract(Name = "SubscribedModSet",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    
    public class SubscribedCollection : AdvancedCollection
    {
        [DataMember] Uri _authorAvatarUrl;
        [DataMember] Guid _collectionID;
        [DataMember] Guid _subscribedAccountId;
        [DataMember] int _subscribers;

        public SubscribedCollection(Guid collectionID, Guid subscribedAccountId, ISupportModding game)
            : base(Guid.NewGuid(), game) {
            _collectionID = collectionID;
            _subscribedAccountId = subscribedAccountId;
        }

        public override bool CanChangeRequiredState => false;
        public Uri AuthorAvatarUrl
        {
            get { return _authorAvatarUrl; }
            set { SetProperty(ref _authorAvatarUrl, value); }
        }
        public Guid CollectionID => _collectionID;
        public int Subscribers
        {
            get { return _subscribers; }
            set { SetProperty(ref _subscribers, value); }
        }
        public Guid SubscribedAccountId
        {
            get { return _subscribedAccountId; }
            private set { SetProperty(ref _subscribedAccountId, value); }
        }
        public override void UpdateFromMod(IMod mod) {}

        protected override ShortGuid GetShortId() => new ShortGuid(CollectionID);

        public Uri GetAuthorUri() => Tools.Transfer.JoinUri(CommonUrls.MainUrl, "u", Author);

        public Task Unsubscribe(IConnectApiHandler api) => api.UnsubscribeCollection(CollectionID);

        public override CustomCollection Clone() {
            var cm = new CustomCollection(Guid.NewGuid(), Game);
            cm.Import(this);

            return cm;
        }

        public override async Task UpdateInfoFromOnline(CollectionModel collection,
            CollectionVersionModel collectionVersion,
            Account author, IContentManager contentList) {
            await base.UpdateInfoFromOnline(collection, collectionVersion, author, contentList).ConfigureAwait(false);

            Subscribers = collection.Subscribers;
            AuthorAvatarUrl = author.Avatar;
            Author = author.DisplayName;

            collectionVersion.Repositories.SyncCollectionLocked(Repositories);
            UpdateServersInfo(collectionVersion);

            await SynchronizeMods(contentList, collectionVersion).ConfigureAwait(false);
        }

        protected override bool AddMod(Mod mod) => false;

        protected override bool RemoveMod(IMod mod) => false;

        public override bool ComparePK(object obj) {
            var modSet = obj as SubscribedCollection;
            if (modSet == null)
                return false;
            return CollectionID == modSet.CollectionID && SubscribedAccountId == modSet.SubscribedAccountId;
        }
    }
}