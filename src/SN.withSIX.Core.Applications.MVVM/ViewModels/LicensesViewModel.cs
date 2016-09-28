// <copyright company="SIX Networks GmbH" file="LicensesViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Core.Applications.MVVM.ViewModels
{
    public interface ILicensesViewModel : IModalScreen {}


    public class LicensesViewModel : ReactiveModalScreen<IShellViewModelBase>, ILicensesViewModel
    {
        public LicensesViewModel(Licenses licenses) {
            DisplayName = "licenses";

            Licenses = licenses;
        }

        public Licenses Licenses { get; protected set; }
    }
}