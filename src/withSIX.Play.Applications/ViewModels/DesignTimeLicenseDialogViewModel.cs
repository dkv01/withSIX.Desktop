// <copyright company="SIX Networks GmbH" file="DesignTimeLicenseDialogViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Play.Applications.ViewModels.Dialogs;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Applications.ViewModels
{
    public class DesignTimeLicenseDialogViewModel : LicenseDialogViewModel, IDesignTimeViewModel
    {
        public DesignTimeLicenseDialogViewModel() {
            var msLicense = new ModSetLicenses("Test Mod") {IsModSetLicensesExpanded = true};
            ModSetLicenses = new List<ModSetLicenses> {msLicense};
            var mod = new Mod(Guid.NewGuid()) {Name = "Test Mod", ModVersion = "1.0.0"};
            var ml = new ModLicense(null, $"{mod.Name} {mod.ModVersion}") {
                IsModLicenseExpanded = true
            };
            msLicense.ModLicenses.Add(ml);
        }
    }
}