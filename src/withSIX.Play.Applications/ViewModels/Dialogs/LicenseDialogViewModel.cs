// <copyright company="SIX Networks GmbH" file="LicenseDialogViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Applications.ViewModels.Dialogs
{
    public interface ILicenseDialogViewModel {}

    
    public class LicenseDialogViewModel : DialogBase, ILicenseDialogViewModel, IDontIC
    {
        protected LicenseDialogViewModel() {}

        public LicenseDialogViewModel(IEnumerable<LicenseInfo> licenses,
            string modSetName) {
            LicensesFailed = "";

            ModSetLicenses = new List<ModSetLicenses>();
            var thisModSetLicenses = new ModSetLicenses(modSetName);
            ModSetLicenses.Add(thisModSetLicenses);

            foreach (var l in licenses)
                HandleLicense(l);

            DisplayName = "License agreements need to be accepted before installation can proceed";
        }

        public LicenseResult DialogResult { get; set; }
        public string LicensesFailed { get; set; }
        public List<ModSetLicenses> ModSetLicenses { get; set; }

        public void Close(LicenseResult result) {
            DialogResult = result;
        }

        void HandleLicense(LicenseInfo mod) {
            var licenseUrl = $"{CommonUrls.PlayUrl}/api/v2/mods/{mod.Id}/license";
            ModSetLicenses[0].ModLicenses.Add(new ModLicense(licenseUrl,
                $"{mod.Title} {mod.Version}"));
        }
    }


    public class LicenseInfo
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
    }

    public enum LicenseResult
    {
        LicensesDeclined,
        LicensesAccepted,
        LicensesError
    }
}