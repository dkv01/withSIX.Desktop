// <copyright company="SIX Networks GmbH" file="IDashboardViewModelBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.ComponentModel;
using Caliburn.Micro;

namespace withSIX.Core.Applications.MVVM.ViewModels
{
    public interface IDashboardViewModelBase : INotifyPropertyChanged, IScreen
    {
        void ShowAbout();
        void ShowSettings();
        void ShowLicenses();
        void ShowCheckForUpdates();
    }
}