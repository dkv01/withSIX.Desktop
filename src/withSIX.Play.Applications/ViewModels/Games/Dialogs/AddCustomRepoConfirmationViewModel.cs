// <copyright company="SIX Networks GmbH" file="AddCustomRepoConfirmationViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI.Legacy;

using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Play.Core.Games.Legacy.Repo;

namespace withSIX.Play.Applications.ViewModels.Games.Dialogs
{
    
    public class AddCustomRepoConfirmationViewModel : DialogBase, IAddCustomRepoConfirmationViewModel
    {
        readonly SixRepo _customRepo;

        public AddCustomRepoConfirmationViewModel(SixRepo customRepo) {
            DisplayName = "Third Party Content";
            _customRepo = customRepo;
            this.SetCommand(x => x.OkCommand).Subscribe(() => {
                HandleDontAskAgain();
                TryClose(true);
            });
            this.SetCommand(x => x.CancelCommand).Subscribe(() => {
                HandleDontAskAgain();
                TryClose(false);
            });
        }

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }
        public bool DontAskAgain { set; get; }
        public string Author => _customRepo.Name;

        void HandleDontAskAgain() {
            if (!DontAskAgain)
                return;
        }
    }
}