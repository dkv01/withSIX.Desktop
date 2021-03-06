// <copyright company="SIX Networks GmbH" file="CustomRepoAvailabilityWarningViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI.Legacy;

using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Games.Legacy.Repo;

namespace withSIX.Play.Applications.ViewModels.Games.Dialogs
{
    public interface ICustomRepoAvailabilityWarningViewModel : IDialog {} // IMetroDialog

    
    public class CustomRepoAvailabilityWarningViewModel : DialogBase, ICustomRepoAvailabilityWarningViewModel
    {
        readonly CustomCollection _customCollection;
        readonly SixRepo _customRepo;
        //ExportFactory<PickContactViewModel> pickContactFactory, Lazy<ModsViewModel> mods
        public CustomRepoAvailabilityWarningViewModel(CustomCollection customCollection) {
            DisplayName = "Third party content";
            _customRepo = customCollection.CustomRepo;
            _customCollection = customCollection;

            this.SetCommand(x => x.OkCommand).Subscribe(() => {
                HandleDontAskAgain();
                TryClose(true);
            });
        }

        public bool RememberedState { set; get; }
        public ReactiveCommand OkCommand { get; private set; }
        public string Author => _customRepo.Name;

        void HandleDontAskAgain() {
            if (!RememberedState)
                return;
            _customCollection.SetRemember();
        }
    }
}