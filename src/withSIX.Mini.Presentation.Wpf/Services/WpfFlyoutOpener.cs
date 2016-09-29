// <copyright company="SIX Networks GmbH" file="WpfFlyoutOpener.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Mini.Applications.MVVM.Services;
using withSIX.Mini.Applications.MVVM.ViewModels;

namespace withSIX.Mini.Presentation.Wpf.Services
{
    public class WpfFlyoutOpener : IFlyoutOpener
    {
        readonly IFlyoutScreen _mainScreen;

        public WpfFlyoutOpener(IFlyoutScreen mainScreen) {
            _mainScreen = mainScreen;
        }

        public void OpenFlyout<T>(T viewModel) where T : class, IFlyoutViewModel {
            _mainScreen.ShowFlyout.Execute(viewModel);
        }
    }
}