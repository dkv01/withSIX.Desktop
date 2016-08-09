// <copyright company="SIX Networks GmbH" file="CollectionSynchronizer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Play.Applications.Services.Infrastructure;
using SN.withSIX.Play.Core.Connect;
using SN.withSIX.Play.Core.Connect.Infrastructure;
using SN.withSIX.Play.Core.Games.Entities;
using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Games.Legacy.Mods;

namespace SN.withSIX.Play.Applications.UseCases.Games
{
    // TODO: Wrap a repository around the contentlist Lists, so that we can perform the locking directly there instead of spread out over our codebase?!
    public class CollectionNotificationHandler : CollectionSynchronizationBase, IAsyncNotificationHandler<LoggedInEvent>,
        IAsyncNotificationHandler<SubscribedToCollection>, IAsyncNotificationHandler<UnsubscribedFromCollection>,
        IAsyncNotificationHandler<CollectionUpdated>, IAsyncNotificationHandler<CollectionVersionAdded>,
        IAsyncNotificationHandler<PublishedCollection>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;
        readonly IGameContext _context;

        public CollectionNotificationHandler(IConnectApiHandler api, IGameContext context,
            IContentManager contentList) : base(api, context, contentList) {
            _api = api;
            _context = context;
            _contentList = contentList;
        }

        public async Task Handle(CollectionUpdated notification) {
            using (var session = await _api.StartSession().ConfigureAwait(false)) {
                await FetchCollectionAndConvert(notification.CollectionId).ConfigureAwait(false);
                await session.Close().ConfigureAwait(false);
            }
        }

        public async Task Handle(CollectionVersionAdded notification) {
            using (var session = await _api.StartSession().ConfigureAwait(false)) {
                await FetchCollectionAndConvert(notification.CollectionId).ConfigureAwait(false);
                await session.Close().ConfigureAwait(false);
            }
        }

        public async Task Handle(LoggedInEvent notification) {
            //Will Crash the application on Failure.
            using (var session = await _api.StartSession().ConfigureAwait(false)) {
                await SynchronizeSubscribedCollections().ConfigureAwait(false);
                await SynchronizePublishedCollections().ConfigureAwait(false);
                await session.Close().ConfigureAwait(false);
            }
        }

        public async Task Handle(PublishedCollection notification) {
            using (var session = await _api.StartSession().ConfigureAwait(false)) {
                await FetchCollectionAndConvert(notification.CollectionId).ConfigureAwait(false);
                await session.Close().ConfigureAwait(false);
            }
        }

        public async Task Handle(SubscribedToCollection notification) {
            using (var session = await _api.StartSession().ConfigureAwait(false)) {
                await FetchCollectionAndConvert(notification.CollectionId).ConfigureAwait(false);
                await session.Close().ConfigureAwait(false);
            }
        }

        public Task Handle(UnsubscribedFromCollection notification) {
            lock (_contentList.SubscribedCollections) {
                var modSet =
                    _contentList.SubscribedCollections.FirstOrDefault(x => x.CollectionID == notification.CollectionId);
                if (modSet != null)
                    _contentList.SubscribedCollections.Remove(modSet);
            }

            return Task.FromResult(0);
        }

        async Task SynchronizeSubscribedCollections() {
            var collections = await _api.GetSubscribedCollections().ConfigureAwait(false);
            foreach (var c in collections)
                await ConvertToSubscribedModSet(c).ConfigureAwait(false);
            lock (_contentList.SubscribedCollections)
                CleanupSubscribedCollections(collections);
        }

        async Task SynchronizePublishedCollections() {
            var collections = await _api.GetOwnedCollections().ConfigureAwait(false);
            foreach (var c in collections)
                await ConvertToPublishedModSet(c).ConfigureAwait(false);
        }

        void CleanupSubscribedCollections(IEnumerable<CollectionModel> collections) {
            var items = _contentList.SubscribedCollections.ToArray();
            foreach (var i in items.Where(i => !collections.Select(x => x.Id).Contains(i.CollectionID)))
                _contentList.SubscribedCollections.Remove(i);
        }

        async Task ConvertToPublishedModSet(CollectionModel collection) {
            //var author = await _api.GetAccount(collection.AuthorId).ConfigureAwait(false);
            var author = new Account(collection.AuthorId); // TODO: author info in collectionmodel..
            var collectionVersion = await _api.GetCollectionVersion(collection.Versions.Last().Id).ConfigureAwait(false);
            var supportModding = _context.Games.FindOrThrow(collection.GameId).Modding();
            var accountId = _api.Me.Account.Id;

            CustomCollection modSet;
            lock (_contentList.CustomCollections) {
                modSet = _contentList.CustomCollections.FirstOrDefault(
                    x => x.PublishedId == collection.Id && x.PublishedAccountId == accountId);
            }

            var newCollection = modSet == null;
            if (newCollection)
                modSet = new CustomCollection(collection.Id, supportModding);

            await modSet.UpdateInfoFromOnline(collection, collectionVersion, author, _contentList).ConfigureAwait(false);

            if (newCollection) {
                lock (_contentList.CustomCollections)
                    _contentList.CustomCollections.Add(modSet);
            }
        }
    }

    public class LoggedInEvent {}

    public class CollectionSynchronizer : CollectionSynchronizationBase,
        IAsyncRequestHandler<ImportCollectionCommand, Unit>,
        IAsyncRequestHandler<RefreshCollectionCommand, Unit>
    {
        readonly IContentManager _contentList;

        public CollectionSynchronizer(IConnectApiHandler api, IGameContext context,
            IContentManager contentList) : base(api, context, contentList) {
            _contentList = contentList;
        }

        public async Task<Unit> Handle(ImportCollectionCommand command) {
            await FetchCollectionAndConvert(command.CollectionId).ConfigureAwait(false);
            return Unit.Value;
        }

        public async Task<Unit> Handle(RefreshCollectionCommand command) {
            SubscribedCollection collection;
            lock (_contentList.SubscribedCollections)
                collection = _contentList.SubscribedCollections.First(x => x.Id == command.CollectionId);

            try {
                await FetchCollectionAndConvert(collection.CollectionID).ConfigureAwait(false);
            } catch (NotFoundException) {
                lock (_contentList.SubscribedCollections)
                    _contentList.SubscribedCollections.Remove(collection);
            }
            return Unit.Value;
        }
    }

    public class FetchCollectionQueryHandler : CollectionSynchronizationBase,
        IAsyncRequestHandler<FetchCollectionQuery, CollectionModel>
    {

        public FetchCollectionQueryHandler(IConnectApiHandler api, IGameContext context,
            IContentManager contentList)
            : base(api, context, contentList) {
        }

        // Had to split up from CollectionSynchronizer due to SimpleInjector bug:
        /*
    Type: System.ArgumentException
    Message:
        Expression of type 'ShortBus.IAsyncRequestHandler`2[SN.withSIX.Play.Applications.UseCases.Games.ImportCollectionCommand,ShortBus.Unit]' cannot be used for constructor parameter of type 'ShortBus.IAsyncRequestHandler`2[SN.withSIX.Play.Applications.UseCases.Games.FetchCollectionCommand,withSIX.Api.Models.Collections.CollectionModel]'
         */

        public Task<CollectionModel> Handle(FetchCollectionQuery request) => FetchCollection(request.CollectionId);
    }

    public abstract class CollectionSynchronizationBase
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;
        readonly IGameContext _context;

        protected CollectionSynchronizationBase(IConnectApiHandler api, IGameContext context,
            IContentManager contentList) {
            _api = api;
            _context = context;
            _contentList = contentList;
        }

        protected async Task FetchCollectionAndConvert(Guid collectionId) {
            var collection = await FetchCollection(collectionId).ConfigureAwait(false);
            await ConvertToSubscribedModSet(collection).ConfigureAwait(false);
        }

        protected Task<CollectionModel> FetchCollection(Guid collectionId) => _api.GetCollection(collectionId);

        protected async Task ConvertToSubscribedModSet(CollectionModel collection) {
            //var author = await _api.GetAccount(collection.AuthorId).ConfigureAwait(false);
            //todo
            var author = new Account(collection.AuthorId);
            var collectionVersion = await _api.GetCollectionVersion(collection.Versions.OrderBy(x => x.Version).Last().Id).ConfigureAwait(false);
            var supportModding = _context.Games.FindOrThrow(collection.GameId).Modding();
            var subscribedAccountId = _api.Me.Account.Id; // TODO: This can break thingsperhaps; null ref exception....

            SubscribedCollection modSet;
            lock (_contentList.SubscribedCollections)
                modSet = _contentList.SubscribedCollections.FirstOrDefault(
                    x => x.CollectionID == collection.Id && x.SubscribedAccountId == subscribedAccountId);

            var newCollection = modSet == null;
            if (newCollection)
                modSet = new SubscribedCollection(collection.Id, subscribedAccountId, supportModding);

            await modSet.UpdateInfoFromOnline(collection, collectionVersion, author, _contentList).ConfigureAwait(false);

            if (newCollection) {
                lock (_contentList.SubscribedCollections)
                    _contentList.SubscribedCollections.Add(modSet);
            }
        }
    }
}