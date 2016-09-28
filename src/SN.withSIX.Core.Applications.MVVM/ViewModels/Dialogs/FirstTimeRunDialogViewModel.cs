// <copyright company="SIX Networks GmbH" file="FirstTimeRunDialogViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Applications.MVVM.ViewModels.Dialogs
{
    public sealed class FirstTimeRunDialogViewModel : ScreenBase<IShellViewModel>
    {
        public FirstTimeRunDialogViewModel() {
            DisplayName = "Welcome to Play withSIX!";
        }


        public void Accept() {
            TryClose(true);
        }
    }
}