// <copyright company="SIX Networks GmbH" file="SearchServerLibraryItemViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel;
using ReactiveUI;
using withSIX.Play.Applications.Extensions;
using withSIX.Play.Applications.ViewModels.Games.Library.LibraryGroup;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Options.Filters;

namespace withSIX.Play.Applications.ViewModels.Games.Library
{
    public class SearchServerLibraryItemViewModel : ServerLibraryItemViewModel, ISearchLibraryItem<Server>,
        IHaveModel<BuiltInServerContainer>
    {
        public SearchServerLibraryItemViewModel(ServerLibraryViewModel library, ArmaServerFilter serverFilter,
            SortDescriptionCollection existing = null,
            string icon = null, ServerLibraryGroupViewModel @group = null,
            bool isFeatured = false, bool doGrouping = false)
            : base(library, serverFilter, existing, icon, @group, isFeatured, doGrouping) {
            Model = new BuiltInServerContainer("Search");
            Icon = SixIconFont.withSIX_icon_Search;
            IsFeatured = true;

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
            Sort = new SortViewModel(ItemsView, sortDatas.Concat(ServersViewModel.Columns).ToArray(), null,
                ServersViewModel.RequiredColumns);
            SetupGrouping();
            SortOrder = 3;
            IsRoot = true;
        }

        public BuiltInServerContainer Model { get; }
        public override ReactiveList<Server> Items { get; } = new ReactiveList<Server>();

        public void UpdateItems(IEnumerable<Server> items) {
            items.SyncCollection(Items);
        }
    }
}