// <copyright company="SIX Networks GmbH" file="ModLicense.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

using withSIX.Core.Helpers;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    public class ModLicense : PropertyChangedBase
    {
        bool _isModLicenseExpanded;

        public ModLicense(string licenseURL, string header) {
            LicenseURL = licenseURL;
            Header = header;
            IsModLicenseExpanded = false;
        }

        public string Header { get; set; }
        public string LicenseURL { get; set; }
        public bool IsModLicenseExpanded
        {
            get { return _isModLicenseExpanded; }
            set { SetProperty(ref _isModLicenseExpanded, value); }
        }
    }

    
    public class UserDeclinedLicenseException : Exception
    {
        public UserDeclinedLicenseException(string message) : base(message) {}
    }

    
    public class LicenseRetrievalException : Exception
    {
        public LicenseRetrievalException(string message) : base(message) {}
    }
}