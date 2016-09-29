// <copyright company="SIX Networks GmbH" file="IFlyoutOpener.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using withSIX.Mini.Applications.MVVM.ViewModels;

namespace withSIX.Mini.Applications.MVVM.Services
{
    public interface IFlyoutOpener
    {
        void OpenFlyout<T>(T viewModel) where T : class, IFlyoutViewModel;
    }

    public interface IFlyoutScreen
    {
        IReactiveCommand ShowFlyout { get; }
    }
}