// <copyright company="SIX Networks GmbH" file="WpfStartupManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using MahApps.Metro.Controls;
using ReactiveUI;
using withSIX.Core.Applications.Infrastructure;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Cache;
using withSIX.Core.Presentation.Legacy;

namespace withSIX.Core.Presentation.Wpf.Legacy
{
    public class WpfStartupManager : StartupManager, IWpfStartupManager
    {
        readonly WpfErrorHandler _wpfErrorHandler;
        readonly MetroWindow dummyWindowForSmartAssembly;

        public WpfStartupManager(ICacheManager cacheManager, IDialogManager dialogManager,
            ISpecialDialogManager specialDialogManager)
            : base(cacheManager) {
            UiRoot.Main = new UiRoot(dialogManager, specialDialogManager);

            // This is the main UserError handler so that when the MainWindow is not yet existing, or no longer existing, we can handle usererrors
            // TODO: We should re-evaluate if handling UserErrors before or after the MainWindow is useful, or that they should either be handled differently
            // or that we should make sure that such errors can only occur during the life cycle of the MainWindow?
            _wpfErrorHandler = new WpfErrorHandler(dialogManager, specialDialogManager);
            UserError.RegisterHandler(x => _wpfErrorHandler.Handler(x));
        }
    }
}