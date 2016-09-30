// <copyright company="SIX Networks GmbH" file="GameInfoOverlayViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Applications.ViewModels.Overlays;

namespace withSIX.Play.Applications.ViewModels.Games.Overlays
{
    
    public class GameInfoOverlayViewModel : OverlayViewModelBase, ISingleton
    {
        readonly Lazy<GamesViewModel> _gvm;

        public GameInfoOverlayViewModel(Lazy<GamesViewModel> gvm) {
            DisplayName = "Game Info";
            _gvm = gvm;
        }

        public GamesViewModel GVM => _gvm.Value;

        
        public void Purchase() {
            GVM.PurchaseGame(GVM.SelectedItem);
        }
    }
}