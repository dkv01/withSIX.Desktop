// <copyright company="SIX Networks GmbH" file="OptionsMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>


namespace withSIX.Play.Applications.ViewModels.Popups
{
    public class OptionsMenu : PopupMenuBase
    {
        readonly OptionsMenuViewModel _optionsMenuViewModelViewModel;

        public OptionsMenu(OptionsMenuViewModel optionsMenuViewModel) {
            _optionsMenuViewModelViewModel = optionsMenuViewModel;
            DisplayName = "Options";
        }

        protected override bool UppercaseFirst => false;

        [MenuItem(icon: SixIconFont.withSIX_icon_Hexagon_Help)]
        public void About() {
            _optionsMenuViewModelViewModel.SwitchAbout();
        }

        [MenuItem(icon: SixIconFont.withSIX_icon_Apps)]
        public void Apps() {
            _optionsMenuViewModelViewModel.SwitchApps();
        }

        [MenuItem(icon: SixIconFont.withSIX_icon_Licenses)]
        public void Licenses() {
            _optionsMenuViewModelViewModel.OpenLicenses();
        }

        [MenuItem(icon: SixIconFont.withSIX_icon_Hexagon_SelfUpdating)]
        public void CheckForUpdates() {
            _optionsMenuViewModelViewModel.ShowCheckForUpdates();
        }

        [MenuItem(icon: SixIconFont.withSIX_icon_Settings)]
        public void Settings() {
            _optionsMenuViewModelViewModel.OpenSettings();
        }
    }
}