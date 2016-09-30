// <copyright company="SIX Networks GmbH" file="ModInfoOverlayViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Linq;
using ReactiveUI;

using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.ViewModels.Overlays;
using withSIX.Play.Core.Connect;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Applications.ViewModels.Games.Overlays
{
    
    public class ModInfoOverlayViewModel : OverlayViewModelBase, ISingleton
    {
        readonly Lazy<ModsViewModel> _mvm;
        bool _isEditingGlobalVersion;
         ObservableAsPropertyHelper<bool> _isInCollection;
         ObservableAsPropertyHelper<IMod> _selectedItem;

        public ModInfoOverlayViewModel(Lazy<ModsViewModel> mvm) {
            DisplayName = "Mod Info";
            _mvm = mvm;
        }

        public IMod SelectedItem => _selectedItem.Value;
        public bool IsInCollection => _isInCollection.Value;
        public bool IsEditingGlobalVersion
        {
            get { return _isEditingGlobalVersion; }
            set { SetProperty(ref _isEditingGlobalVersion, value); }
        }

        protected override void OnInitialize() {
            base.OnInitialize();

            _selectedItem = _mvm.Value.LibraryVM.WhenAnyValue(x => x.SelectedItem.SelectedItem)
                .OfType<IMod>()
                .ToProperty(this, x => x.SelectedItem);

            _isInCollection = this.WhenAny(x => x.SelectedItem, x => x.Value is ToggleableModProxy)
                .ToProperty(this, x => x.IsInCollection);
        }

        
        public void ShowInfo() {
            BrowserHelper.TryOpenUrlIntegrated(SelectedItem.ProfileUrl());
        }

        
        public void ShowHomepage() {
            _mvm.Value.ViewHomepageAction(SelectedItem);
        }

        
        public void EditGlobalVersion() {
            IsEditingGlobalVersion = true;
            //var mod = SelectedItem;
            //GVM.LibraryVM.SelectNetwork();
            //GVM.LibraryVM.SelectedItem.SelectedItem = mod.ToMod();
        }

        
        public void CloseEditGlobalVersion() {
            IsEditingGlobalVersion = false;
        }

        
        public void ShowChangelog() {
            _mvm.Value.ViewChangelogAction(SelectedItem);
        }
    }
}