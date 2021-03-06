﻿// <copyright company="SIX Networks GmbH" file="AppOverlayViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using ReactiveUI;

using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Play.Applications.Extensions;
using withSIX.Play.Core.Options;
using withSIX.Play.Core.Options.Entries;
using ReactiveCommand = ReactiveUI.Legacy.ReactiveCommand;

namespace withSIX.Play.Applications.ViewModels.Overlays
{
    
    public class AppOverlayViewModel : OverlayViewModelBase
    {
        readonly IDialogManager _dialogManager;
        readonly object _externalAppsLock = new Object();
        readonly UserSettings _userSettings;

        public AppOverlayViewModel(UserSettings settings, IDialogManager dialogManager) {
            DisplayName = "External Apps";
            _userSettings = settings;
            _dialogManager = dialogManager;

            this.SetCommand(x => x.AddAppCommand).RegisterAsyncTask(AddApp).Subscribe();
            this.SetCommand(x => x.RemoveAppCommand).Subscribe(x => RemoveApp((ExternalApp) x));

            ExternalApps.EnableCollectionSynchronization(_externalAppsLock);
        }

        public ReactiveCommand AddAppCommand { get; protected set; }
        public ReactiveCommand RemoveAppCommand { get; protected set; }
        public ReactiveList<ExternalApp> ExternalApps
        {
            get { return _userSettings.AppOptions.ExternalApps; }
            set { _userSettings.AppOptions.ExternalApps = value; }
        }

        async Task AddApp() {
            var selectedFile = await _dialogManager.BrowseForFile(null, null, ".exe", true);
            if (selectedFile == null)
                return;

            await Task.Run(() => {
                var newApp = new ExternalApp(Path.GetFileNameWithoutExtension(selectedFile), selectedFile, "", false,
                    StartupType.Any);
                ExternalApps.AddLocked(newApp);
            });
        }

        void RemoveApp(ExternalApp app) {
            ExternalApps.RemoveLocked(app);
        }
    }
}