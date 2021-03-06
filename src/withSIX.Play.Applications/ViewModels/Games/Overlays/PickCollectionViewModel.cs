﻿// <copyright company="SIX Networks GmbH" file="PickCollectionViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using ReactiveUI;
using MediatR;

using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Play.Applications.Extensions;
using withSIX.Play.Applications.UseCases;
using withSIX.Play.Applications.ViewModels.Overlays;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Mods;
using CollectionExtensions = withSIX.Core.Extensions.CollectionExtensions;
using ReactiveCommand = ReactiveUI.Legacy.ReactiveCommand;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Applications.ViewModels.Games.Overlays
{
    
    public class PickCollectionViewModel : OverlayViewModelBase
    {
        readonly object _itemsLock = new object();
        readonly IMediator _mediator;
        string _filterText;
        bool _isExecuting;
        ReactiveList<PickCollectionDataModel> _selectedItems;

        public PickCollectionViewModel(IMediator mediator) {
            _mediator = mediator;
            Items = new ReactiveList<PickCollectionDataModel>();
            UiHelper.TryOnUiThread(() => {
                Items.EnableCollectionSynchronization(_itemsLock);
                ItemsView =
                    Items.CreateCollectionView(new List<SortDescription> {
                        new SortDescription("Name", ListSortDirection.Ascending)
                    }, null, new List<string> {"Name"}, OnFilter, true);
            });
            SelectedItems = new ReactiveList<PickCollectionDataModel>();

            this.WhenAnyValue(x => x.FilterText)
                .Throttle(Common.AppCommon.DefaultFilterDelay)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ItemsView.TryRefreshIfHasView());

            this.SetCommand(x => x.OkCommand,
                this.WhenAnyValue(x => x.SelectedItems.Count).Select(x => x > 0), false)
                .RegisterAsyncTask(Process)
                .Subscribe();

            OkCommand.IsExecuting.Subscribe(x => IsExecuting = x);

            DisplayName = "Add mod to Collections";
        }

        public string FilterText
        {
            get { return _filterText; }
            set { SetProperty(ref _filterText, value); }
        }
        public ICollectionView ItemsView { get; private set; }
        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveList<PickCollectionDataModel> Items { get; }
        public ReactiveList<PickCollectionDataModel> SelectedItems
        {
            get { return _selectedItems; }
            set { SetProperty(ref _selectedItems, value); }
        }
        public IMod Content { get; private set; }
        public bool IsExecuting
        {
            get { return _isExecuting; }
            set { SetProperty(ref _isExecuting, value); }
        }

        async Task Process() {
            await
                _mediator.RequestAsyncWrapped(new AddContentToCollectionsCommand(Content.Id,
                    SelectedItems.Select(x => x.Id).ToArray())).ConfigureAwait(false);
            TryClose();
        }

        bool OnFilter(object obj) {
            var item = obj as PickCollectionDataModel;
            return item != null &&
                   (string.IsNullOrWhiteSpace(FilterText) ||
                    item.Name.NullSafeContainsIgnoreCase(FilterText));
        }

        
        public void SelectionChanged(SelectionChangedEventArgs args) {
            lock (SelectedItems) {
                CollectionExtensions.RemoveAll(SelectedItems, args.RemovedItems.Cast<PickCollectionDataModel>().ToArray());
                SelectedItems.AddRange(args.AddedItems.Cast<PickCollectionDataModel>());
            }
        }

        public void SetContent(IMod content) {
            Content = content;
        }

        static string GetTypeString(IContent content) {
            if (content is Collection)
                return "collection!";
            return (content is IMod ? "mod!" : "mission!");
        }

        public void SetCurrent(PickCollectionDataModel item) {
            ItemsView.MoveCurrentTo(item);
            if (item != null)
                SelectedItems.AddLocked(item);
            else
                SelectedItems.ClearLocked();
        }

        public void LoadItems(IEnumerable<PickCollectionDataModel> items) {
            items.SyncCollection(Items);
        }

        
        public void Cancel() {
            TryClose();
        }
    }
}