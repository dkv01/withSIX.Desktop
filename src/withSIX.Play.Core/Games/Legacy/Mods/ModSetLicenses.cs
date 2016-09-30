// <copyright company="SIX Networks GmbH" file="ModSetLicenses.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    public class ModSetLicenses : PropertyChangedBase
    {
        bool _isModSetLicensesExpanded;

        public ModSetLicenses(string header) {
            Header = header;
            IsModSetLicensesExpanded = true;
            ModLicenses = new List<ModLicense>();
        }

        public string Header { get; set; }
        public int NumLicenses => ModLicenses.Count;
        public bool IsModSetLicensesExpanded
        {
            get { return _isModSetLicensesExpanded; }
            set { SetProperty(ref _isModSetLicensesExpanded, value); }
        }
        public List<ModLicense> ModLicenses { get; set; }
    }
}