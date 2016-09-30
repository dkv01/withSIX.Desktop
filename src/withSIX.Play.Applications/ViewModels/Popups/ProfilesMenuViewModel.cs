// <copyright company="SIX Networks GmbH" file="ProfilesMenuViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using MediatR;

using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation.Wpf.Extensions;
using withSIX.Play.Applications.DataModels.Profiles;
using withSIX.Play.Applications.UseCases;
using withSIX.Play.Applications.UseCases.Profiles;
using ReactiveCommand = ReactiveUI.Legacy.ReactiveCommand;
using Unit = System.Reactive.Unit;

namespace withSIX.Play.Applications.ViewModels.Popups
{
    
    public class ProfilesMenuViewModel : PopupBase, IDisposable
    {
        readonly IDialogManager _dialogManager;
        private readonly ISpecialDialogManager _specialDialogManager;
        readonly IMediator _mediator;
        ProfileDataModel _activeProfile;
        bool _showProfilesMenu;

        public ProfilesMenuViewModel(IMediator mediator, IDialogManager dialogManager, ISpecialDialogManager specialDialogManager) {
            _mediator = mediator;
            _dialogManager = dialogManager;
            _specialDialogManager = specialDialogManager;

            this.SetCommand(x => x.ProfilesMenuCommand).Subscribe(x => ShowProfilesMenu = !ShowProfilesMenu);
            ReactiveUI.ReactiveCommand.CreateAsyncTask(x => OpenNewProfileDialog())
                .SetNewCommand(this, x => x.AddNewProfileCommand)
                .Subscribe();
        }

        public ReactiveCommand ProfilesMenuCommand { get; private set; }
        public ReactiveCommand<Unit> AddNewProfileCommand { get; private set; }
        public bool ShowProfilesMenu
        {
            get { return _showProfilesMenu; }
            set { SetProperty(ref _showProfilesMenu, value); }
        }
        public IReactiveDerivedList<ProfileDataModel> Profiles { get; internal set; }
        public ProfileDataModel ActiveProfile
        {
            get { return _activeProfile; }
            set { SetProperty(ref _activeProfile, value); }
        }

        public void Dispose() {
            Dispose(true);
        }

        
        public void DeleteProfile(ProfileDataModel profile) {
            ShowProfilesMenu = false;
            var result = _dialogManager.MessageBox(new MessageBoxDialogParams(
                $"Are you sure you want to delete the profile '{profile.Name}'?",
                "Confirm delete profile", SixMessageBoxButton.YesNo)).WaitSpecial();

            if (result.IsYes())
                _mediator.Send(new DeleteProfileCommand(profile.Id));
        }

        
        public void SwitchProfile(ProfileDataModel profile) {
            ShowProfilesMenu = false;
            _mediator.Send(new SwitchProfileCommand(profile.Id));
        }

        async Task OpenNewProfileDialog() {
            ShowProfilesMenu = false;
            using (var vm = _mediator.Send(new ShowNewProfileDialogQuery()))
                await _specialDialogManager.ShowDialog(vm).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing)
                Profiles.Dispose();
        }
    }
}