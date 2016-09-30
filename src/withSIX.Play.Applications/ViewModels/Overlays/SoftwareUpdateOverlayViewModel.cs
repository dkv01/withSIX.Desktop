// <copyright company="SIX Networks GmbH" file="SoftwareUpdateOverlayViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Linq;
using ReactiveUI;
using withSIX.Play.Applications.Services;
using withSIX.Play.Core.Connect;

namespace withSIX.Play.Applications.ViewModels.Overlays
{
    
    public abstract class SoftwareUpdateOverlayViewModelBase : OverlayViewModelBase
    {
        bool _canCheckForUpdates;

        protected SoftwareUpdateOverlayViewModelBase(SettingsViewModel settingsViewModel) {
            DisplayName = "Check for Updates";

            SettingsVM = settingsViewModel;
        }

        public SettingsViewModel SettingsVM { get; private set; }
        public bool CanCheckForUpdates
        {
            get { return _canCheckForUpdates; }
            set { SetProperty(ref _canCheckForUpdates, value); }
        }

        
        public void ViewChangelog() {
            TryClose();
            BrowserHelper.TryOpenUrlIntegrated(CommonUrls.ChangelogUrl);
        }
    }

    
    public class SoftwareUpdateOverlayViewModel : SoftwareUpdateOverlayViewModelBase
    {
        public SoftwareUpdateOverlayViewModel(SettingsViewModel settingsViewModel, ISoftwareUpdate softwareUpdate)
            : base(settingsViewModel) {
            SoftwareUpdate = softwareUpdate;

            this.WhenAnyValue(x => x.IsActive)
                .Where(x => x)
                .Subscribe(x => CheckForUpdates());
        }

        public ISoftwareUpdate SoftwareUpdate { get; }

        
        public async void CheckForUpdates() {
            CanCheckForUpdates = false;
            try {
                await SoftwareUpdate.TryCheckForUpdates().ConfigureAwait(false);
            } finally {
                CanCheckForUpdates = true;
            }
        }


        
        public void Restart() {
            SoftwareUpdate.UpdateAndExitIfNotBusy(true);
        }


        
        public void InstallSingleExe() {
            SoftwareUpdate.InstallAndExitIfNotBusy();
        }
    }
}