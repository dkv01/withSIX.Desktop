// <copyright company="SIX Networks GmbH" file="CollectionSettingsMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using MoreLinq;
using MediatR;

using withSIX.Api.Models.Collections;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.MVVM.Attributes;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Play.Applications.UseCases.Games;
using SN.withSIX.Play.Applications.ViewModels.Games.Library;
using SN.withSIX.Play.Core.Games.Legacy.Mods;

namespace SN.withSIX.Play.Applications.ViewModels.Games
{
    public class CollectionSettingsMenu : PopupMenuBase<CollectionLibraryItemViewModel>
    {
        readonly IDialogManager _dialogManager;
        readonly IMediator _mediator;

        public CollectionSettingsMenu(IDialogManager dialogManager, IMediator mediator,
            UploadCollection uploadCollection) {
            _dialogManager = dialogManager;
            _mediator = mediator;

            StateChangeMenu = new CollectionStateChangeMenu(uploadCollection);
            RegisterSubMenu(StateChangeMenu);
            Header = "Collection settings";
        }

        public CollectionStateChangeMenu StateChangeMenu { get; }

        protected override void UpdateItemsFor(CollectionLibraryItemViewModel item) {
            base.UpdateItemsFor(item);
            Items.Where(x => x.AsyncAction == Unpublish || x.AsyncAction == ChangeState)
                .ForEach(x => x.IsVisible = item.IsHosted && item is CustomCollectionLibraryItemViewModel);

            GetAsyncItem(RefreshCollection)
                .IsVisible = item.IsSubscribedCollection;

            GetItem(ViewWebPage)
                .IsVisible = item.IsHosted;

            var cc = item.Model as CustomCollection;
            var allowChanges = cc != null && cc.AllowChanges();
            GetItem(LockAllVersions)
                .IsVisible = allowChanges && !cc.AllModsLocked;
            GetItem(UnlockAllVersions)
                .IsVisible = allowChanges && cc.AllModsLocked;
            Items.Where(x => x.Action == UpdateAllLockedVersions)
                .ForEach(x => x.IsVisible = allowChanges);

            GetAsyncItem(Remove)
                .IsVisible = item.IsHosted &&
                             (item is CustomCollectionLibraryItemViewModel ||
                              item is CustomRepoCollectionLibraryItemViewModel);

            GetItem(MakeAllModsRequired)
                .IsVisible = allowChanges && !cc.AllModsRequired;
            GetItem(MakeAllModsOptional)
                .IsVisible = allowChanges && cc.AllModsRequired;
        }

        [MenuItem("Refresh")]
        public Task RefreshCollection(CollectionLibraryItemViewModel item) => _mediator.RequestAsyncWrapped(new RefreshCollectionCommand(item.Model.Id));

        // workaround for menu not disappearing..
        [MenuItem]
        public Task ChangeState(CollectionLibraryItemViewModel item) => Task.Run(() => {
            StateChangeMenu.SetNextItem((CustomCollectionLibraryItemViewModel)item);
            StateChangeMenu.IsOpen = true;
        });

        [MenuItem]
        public async Task Unpublish(CollectionLibraryItemViewModel item) {
            if (
                (await _dialogManager.MessageBox(
                    new MessageBoxDialogParams(
                        "Are you sure you want to stop sharing this Collection?",
                        "Stop sharing collection?", SixMessageBoxButton.YesNo))).IsYes())
                await _mediator.RequestAsyncWrapped(new UnpublishCollectionCommand(item.Model.Id));
        }

        [MenuItem]
        public async Task Remove(CollectionLibraryItemViewModel item) {
            if (
                (await _dialogManager.MessageBox(
                    new MessageBoxDialogParams(
                        "Are you sure you want to delete this synced collection? This cannot be undone",
                        "Delete synced collection?", SixMessageBoxButton.YesNo))).IsYes())
                await _mediator.RequestAsyncWrapped(new DeleteCollectionCommand(item.Model.Id));
        }

        [MenuItem]
        public void ViewWebPage(CollectionLibraryItemViewModel item) {
            item.ViewOnline();
        }

        [MenuItem]
        public void LockAllVersions(CollectionLibraryItemViewModel item) {
            var model = item.Model as CustomCollection;
            model.Lock();
        }

        [MenuItem]
        public void UnlockAllVersions(CollectionLibraryItemViewModel item) {
            var model = item.Model as CustomCollection;
            model.Unlock();
        }

        [MenuItem]
        public void UpdateAllLockedVersions(CollectionLibraryItemViewModel item) {
            var model = item.Model as CustomCollection;
            model.UpdateAllLocked();
        }

        [MenuItem]
        public void MakeAllModsRequired(CollectionLibraryItemViewModel item) {
            var model = item.Model as CustomCollection;
            model.Require();
        }

        [MenuItem]
        public void MakeAllModsOptional(CollectionLibraryItemViewModel item) {
            var model = item.Model as CustomCollection;
            model.Unrequire();
        }
    }

    public class CollectionStateChangeMenu : PopupMenuBase<CustomCollectionLibraryItemViewModel>
    {
        readonly UploadCollection _uploadCollection;

        public CollectionStateChangeMenu(UploadCollection uploadCollection) {
            _uploadCollection = uploadCollection;
            Header = "Change state";
        }

        [MenuItem]
        public Task Private(CustomCollectionLibraryItemViewModel item) => ChangeState(item, CollectionScope.Private);

        [MenuItem]
        public Task Unlisted(CustomCollectionLibraryItemViewModel item) => ChangeState(item, CollectionScope.Unlisted);

        [MenuItem]
        public Task Public(CustomCollectionLibraryItemViewModel item) => ChangeState(item, CollectionScope.Public);

        Task ChangeState(CustomCollectionLibraryItemViewModel collection, CollectionScope scope) => _uploadCollection.ChangeScope(collection, scope);

        protected override void UpdateItemsFor(CustomCollectionLibraryItemViewModel item) {
            base.UpdateItemsFor(item);

            switch (item.Model.PublishingScope) {
            case CollectionScope.Private: {
                GetAsyncItem(Private)
                    .IsVisible = false;
                Items.Where(x => x.AsyncAction == Public || x.AsyncAction == Unlisted)
                    .ForEach(x => x.IsVisible = true);
                break;
            }
            case CollectionScope.Unlisted: {
                GetAsyncItem(Unlisted)
                    .IsVisible = false;
                Items.Where(x => x.AsyncAction == Public || x.AsyncAction == Private)
                    .ForEach(x => x.IsVisible = true);
                break;
            }
            case CollectionScope.Public: {
                GetAsyncItem(Public)
                    .IsVisible = false;
                Items.Where(x => x.AsyncAction == Private || x.AsyncAction == Unlisted)
                    .ForEach(x => x.IsVisible = true);
                break;
            }
            }
        }
    }
}