﻿// <copyright company="SIX Networks GmbH" file="CollectionCreatedViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Reactive;
using ReactiveUI;

using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.ViewModels.Games.Library;
using ReactiveCommand = ReactiveUI.Legacy.ReactiveCommand;

namespace withSIX.Play.Applications.ViewModels.Games.Dialogs
{
    public interface ICollectionCreatedViewModel {}

    
    public class CollectionCreatedViewModel : DialogBase, ICollectionCreatedViewModel
    {
        readonly Lazy<ModsViewModel> _mods;

        public CollectionCreatedViewModel(Lazy<ModsViewModel> mods) {
            _mods = mods;

            this.SetCommand(x => x.OkCommand).Subscribe(() => TryClose());
            DisplayName = "Share the links to the published collection";
        }

        public Uri PwsUrl { get; private set; }
        public Uri OnlineUrl { get; private set; }
        public CustomCollectionLibraryItemViewModel Collection { get; private set; }
        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand<Unit> ShareCommand { get; private set; }

        public void SetCollection(CustomCollectionLibraryItemViewModel collection) {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            Collection = collection;
            OnlineUrl = collection.Model.ProfileUrl();
            PwsUrl = collection.Model.GetPwsUri();
        }
    }
}