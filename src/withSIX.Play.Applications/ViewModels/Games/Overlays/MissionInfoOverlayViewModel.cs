// <copyright company="SIX Networks GmbH" file="MissionInfoOverlayViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Applications.ViewModels.Overlays;

namespace withSIX.Play.Applications.ViewModels.Games.Overlays
{
    
    public class MissionInfoOverlayViewModel : OverlayViewModelBase, ISingleton
    {
        readonly Lazy<MissionsViewModel> _mvm;

        public MissionInfoOverlayViewModel(Lazy<MissionsViewModel> mvm) {
            DisplayName = "Mission Info";
            _mvm = mvm;
        }

        public MissionsViewModel GVM => _mvm.Value;
    }
}