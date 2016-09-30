// <copyright company="SIX Networks GmbH" file="DesignTimeSettingsViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Applications.MVVM.ViewModels;

namespace withSIX.Play.Applications.ViewModels.Overlays
{
    public class DesignTimeSettingsViewModel : SettingsViewModel, IDesignTimeViewModel
    {
        public DesignTimeSettingsViewModel() {
            SetupPropertyGrid();
        }
    }
}