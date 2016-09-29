// <copyright company="SIX Networks GmbH" file="IShellViewModelBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using Caliburn.Micro;

namespace withSIX.Core.Applications.MVVM.ViewModels
{
    public interface IShellViewModelBase : IShellViewModel, IModalShell, IConductActiveItem
    {
        bool MainContentEnabled { get; }
        void ShowDashboard();
    }
}