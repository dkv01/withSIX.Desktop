// <copyright company="SIX Networks GmbH" file="ServerInfoOverlayViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Applications.ViewModels.Overlays;

namespace withSIX.Play.Applications.ViewModels.Games.Overlays
{
    
    public class ServerInfoOverlayViewModel : OverlayViewModelBase, ISingleton
    {
        readonly Lazy<ServersViewModel> _svm;

        public ServerInfoOverlayViewModel(Lazy<ServersViewModel> svm) {
            _svm = svm;
            DisplayName = "Server Info /";
            SmallHeader = true;
        }

        public ServersViewModel SVM => _svm.Value;
    }
}