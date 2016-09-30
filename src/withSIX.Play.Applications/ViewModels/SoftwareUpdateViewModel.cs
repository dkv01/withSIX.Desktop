// <copyright company="SIX Networks GmbH" file="SoftwareUpdateViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Linq;
using ReactiveUI;

using withSIX.Core;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.Services;
using withSIX.Play.Core.Connect;

namespace withSIX.Play.Applications.ViewModels
{
    
    public interface ISoftwareUpdateViewModel : IModalScreen {}

    
    public class SoftwareUpdateViewModel : ReactiveModalScreen<IShellViewModelFullBase>, ISoftwareUpdateViewModel
    {
        public SoftwareUpdateViewModel(ISoftwareUpdate softwareUpdate) {
            DisplayName = "check for updates";
            SoftwareUpdate = softwareUpdate;

            this.WhenAnyValue(x => x.IsActive)
                .Where(x => x)
                .Subscribe(x => CheckForUpdates());
        }

        public ISoftwareUpdate SoftwareUpdate { get; set; }

        
        public async void CheckForUpdates() {
            await SoftwareUpdate.TryCheckForUpdates().ConfigureAwait(false);
        }

        
        public void ViewChangelog() {
            BrowserHelper.TryOpenUrlIntegrated(CommonUrls.ChangelogUrl);
        }


        
        public void Restart() {
            SoftwareUpdate.UpdateAndExitIfNotBusy();
        }
    }
}