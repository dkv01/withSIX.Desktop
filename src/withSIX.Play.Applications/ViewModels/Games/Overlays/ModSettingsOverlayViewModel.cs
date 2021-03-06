// <copyright company="SIX Networks GmbH" file="ModSettingsOverlayViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.ViewModels.Overlays;
using withSIX.Play.Core.Games.Legacy.Helpers;

namespace withSIX.Play.Applications.ViewModels.Games.Overlays
{
    
    public class ModSettingsOverlayViewModel : OverlayViewModelBase, ISingleton
    {
        readonly Lazy<ModsViewModel> _gvm;

        public ModSettingsOverlayViewModel(Lazy<ModsViewModel> gvm) {
            DisplayName = "Mod Settings";
            _gvm = gvm;
        }

        public ModsViewModel GVM => _gvm.Value;

        
        public void SaveUserconfig() {
            var item = GVM.LibraryVM.SelectedItem.SelectedItem;
            var mod = item.ToMod();
            mod.UserConfig.Save();
        }
    }
}