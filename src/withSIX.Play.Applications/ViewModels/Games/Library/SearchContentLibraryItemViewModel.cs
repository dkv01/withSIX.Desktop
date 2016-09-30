// <copyright company="SIX Networks GmbH" file="SearchContentLibraryItemViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ReactiveUI;
using withSIX.Core.Applications;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Play.Applications.Extensions;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Options.Filters;

namespace withSIX.Play.Applications.ViewModels.Games.Library
{
    public abstract class SearchContentLibraryItemViewModel : ContentLibraryItemViewModel,
        IHaveModel<BuiltInContentContainer>,
        ISearchLibraryItem<IContent>
    {
        protected SearchContentLibraryItemViewModel(LibraryRootViewModel library)
            : base(library, null) {
            Model = new BuiltInContentContainer("Search");
            Icon = SixIconFont.withSIX_icon_Search;
            IsFeatured = true;
            Filter = new ModLibraryFilter();
            SetupFilterChanged();

            UiHelper.TryOnUiThread(() => {
                Items.EnableCollectionSynchronization(ItemsLock);
                _itemsView =
                    Items.CreateCollectionView(
                        new[] {
                            new SortDescription("SearchScore", ListSortDirection.Descending)
                        },
                        null, null, Filter.Handler, true);
            });
            var sortDatas = new[] {
                new SortData {
                    DisplayName = "Search score",
                    Value = "SearchScore",
                    SortDirection = ListSortDirection.Descending
                }
            };
            Sort = new SortViewModel(ItemsView, sortDatas.Concat(Columns).ToArray(), null, RequiredColumns);
            SetupGrouping();
            SortOrder = 3;
            IsRoot = true;
        }

        public BuiltInContentContainer Model { get; }
        public override ReactiveList<IContent> Items { get; } = new ReactiveList<IContent>();

        public void UpdateItems(IEnumerable<IContent> items) {
            items.SyncCollection(Items);
        }
    }
}