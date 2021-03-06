// <copyright company="SIX Networks GmbH" file="AboutViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using ReactiveUI.Legacy;

using withSIX.Core;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.Services;

namespace withSIX.Play.Applications.ViewModels.Overlays
{
    
    public class AboutViewModel : OverlayViewModelBase
    {
        readonly About _about;

        public AboutViewModel() {
            _about = new About();
            DisplayName = "About";

            this.SetCommand(x => x.ApplicationLicensesCommand);
        }

        public string Disclaimer => _about.Disclaimer;
        public string Components => _about.Components;
        public Version AppVersion => _about.AppVersion;
        public string ProductVersion => _about.ProductVersion;
        public bool DiagnosticsModeEnabled => Common.Flags.Verbose;
        [Browsable(false)]
        public ReactiveCommand ApplicationLicensesCommand { get; private set; }
    }
}