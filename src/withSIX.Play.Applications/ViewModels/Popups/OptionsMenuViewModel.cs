// <copyright company="SIX Networks GmbH" file="OptionsMenuViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using ReactiveUI.Legacy;

using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.Services;
using withSIX.Play.Applications.ViewModels.Overlays;

namespace withSIX.Play.Applications.ViewModels.Popups
{
    
    public class OptionsMenuViewModel : ViewModelBase
    {
        readonly IViewModelFactory _factory;
        readonly SettingsViewModel _settings;
        readonly IPlayShellViewModel _shellViewModel;
        readonly ISoftwareUpdate _softwareUpdate;

        public OptionsMenuViewModel(IPlayShellViewModel playShellViewModel) {
            _shellViewModel = playShellViewModel;
            _softwareUpdate = _shellViewModel.SoftwareUpdate;
            _factory = _shellViewModel.Factory;
            _settings = _shellViewModel.Settings;

            OptionsMenu = new OptionsMenu(this);

            this.SetCommand(x => x.OptionsMenuCommand).Subscribe(x => OptionsMenu.IsOpen = !OptionsMenu.IsOpen);
        }

        public ReactiveCommand OptionsMenuCommand { get; private set; }
        public OptionsMenu OptionsMenu { get; protected set; }

        public void SwitchAbout() {
            var about = _factory.CreateAbout().Value;
            about.ApplicationLicensesCommand.Subscribe(y => OpenLicenses());
            _shellViewModel.ShowOverlay(about);
        }

        public void SwitchApps() {
            _shellViewModel.ShowOverlay(_factory.CreateApps().Value);
        }

        public void OpenLicenses() {
            _shellViewModel.ShowOverlay(_factory.CreateApplicationLicenses().Value);
        }

        public void ShowCheckForUpdates() {
            _softwareUpdate.NewVersionInstalled = false;
            _shellViewModel.ShowOverlay(_factory.CreateSoftwareUpdate().Value);
        }

        public void OpenSettings() {
            _shellViewModel.ShowOverlay(_settings);
        }
    }
}