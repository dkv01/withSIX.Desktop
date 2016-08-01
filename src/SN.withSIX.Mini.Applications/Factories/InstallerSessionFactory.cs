// <copyright company="SIX Networks GmbH" file="InstallerSessionFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Mini.Applications.Factories
{
    public class InstallerSessionFactory : IINstallerSessionFactory, IApplicationService
    {
        private readonly IAuthProvider _authProvider;
        private readonly ISteamDownloader _steamDownloader;
        private readonly IDbContextLocator _contextLocator;
        readonly IContentEngine _contentEngine;
        readonly Func<bool> _isPremium;
        readonly IToolsCheat _toolsInstaller;

        public InstallerSessionFactory(Func<bool> isPremium, IToolsCheat toolsInstaller,
            IContentEngine contentEngine, IAuthProvider authProvider, ISteamDownloader steamDownloader, IDbContextLocator contextLocator) {
            _isPremium = isPremium;
            _toolsInstaller = toolsInstaller;
            _contentEngine = contentEngine;
            _authProvider = authProvider;
            _steamDownloader = steamDownloader;
            _contextLocator = contextLocator;
        }

        public IInstallerSession Create(
            IInstallContentAction<IInstallableContent> action,
            Func<ProgressInfo, Task> progress) {
            switch (action.InstallerType) {
            case InstallerType.Synq:
                return new SynqInstallerSession(action, _toolsInstaller, _isPremium, progress, _contentEngine,
                    _authProvider, _steamDownloader, _contextLocator);
            default:
                throw new NotSupportedException(action.InstallerType + " is not supported!");
            }
        }

        public IUninstallSession CreateUninstaller(IUninstallContentAction2<IUninstallableContent> action)
            => new SynqUninstallerSession(action);
    }
}