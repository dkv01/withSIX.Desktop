// <copyright company="SIX Networks GmbH" file="InstallerSessionFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.ContentEngine.Core;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Steam.Core.Services;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Mini.Applications.Factories
{
    public delegate bool PremiumDelegate();

    public class InstallerSessionFactory : IINstallerSessionFactory, IApplicationService
    {
        private readonly ISteamHelperRunner _steamHelperRunner;
        private readonly Func<IInstallerSession> _fact;

        public InstallerSessionFactory(ISteamHelperRunner steamHelperRunner, Func<IInstallerSession> fact) {
            _steamHelperRunner = steamHelperRunner;
            _fact = fact;
        }

        public IInstallerSession Create(
            IInstallContentAction<IInstallableContent> action,
            Func<ProgressInfo, Task> progress) {
            switch (action.InstallerType) {
            case InstallerType.Synq:
                var i = _fact();
                i.Activate(action, progress);
                return i;
            default:
                throw new NotSupportedException(action.InstallerType + " is not supported!");
            }
        }

        public IUninstallSession CreateUninstaller(IUninstallContentAction2<IUninstallableContent> action)
            => new UninstallerSession(action, _steamHelperRunner);
    }
}