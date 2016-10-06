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
        private readonly IAuthProvider _authProvider;
        readonly IContentEngine _contentEngine;
        readonly PremiumDelegate _isPremium;
        readonly IToolsCheat _toolsInstaller;
        private readonly IExternalFileDownloader _dl;
        private readonly ISteamHelperRunner _steamHelperRunner;

        public InstallerSessionFactory(PremiumDelegate isPremium, IToolsCheat toolsInstaller,
            IContentEngine contentEngine, IAuthProvider authProvider, IExternalFileDownloader dl, ISteamHelperRunner steamHelperRunner) {
            _isPremium = isPremium;
            _toolsInstaller = toolsInstaller;
            _contentEngine = contentEngine;
            _authProvider = authProvider;
            _dl = dl;
            _steamHelperRunner = steamHelperRunner;
        }

        public IInstallerSession Create(
            IInstallContentAction<IInstallableContent> action,
            Func<ProgressInfo, Task> progress) {
            switch (action.InstallerType) {
            case InstallerType.Synq:
                return new InstallerSession(action, _toolsInstaller, _isPremium, progress, _contentEngine,
                    _authProvider, _dl, _steamHelperRunner);
            default:
                throw new NotSupportedException(action.InstallerType + " is not supported!");
            }
        }

        public IUninstallSession CreateUninstaller(IUninstallContentAction2<IUninstallableContent> action)
            => new UninstallerSession(action, _steamHelperRunner);
    }
}