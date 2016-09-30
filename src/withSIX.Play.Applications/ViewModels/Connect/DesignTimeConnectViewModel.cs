// <copyright company="SIX Networks GmbH" file="DesignTimeConnectViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using Caliburn.Micro;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Play.Applications.Services;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.ViewModels.Connect
{
    public class DesignTimeConnectViewModel : ConnectViewModel, IDesignTimeViewModel
    {
        public DesignTimeConnectViewModel()
            : base(
                IoC.Get<ContactList>(), null, null, null,
                IoC.Get<UserSettings>(),
                IoC.Get<IEventAggregator>()) {}
    }
}