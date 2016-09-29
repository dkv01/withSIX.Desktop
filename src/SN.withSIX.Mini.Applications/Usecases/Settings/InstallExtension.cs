// <copyright company="SIX Networks GmbH" file="InstallExtension.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Usecases.Settings
{
    public class InstallExtension : IAsyncVoidCommand {}

    public class RemoveExtension : IAsyncVoidCommand {}

    public class ExtensionHandler : DbCommandBase, IAsyncVoidCommandHandler<InstallExtension>,
        IAsyncVoidCommandHandler<RemoveExtension>
    {
        private readonly IAbsoluteDirectoryPath _destination =
            Common.Paths.LocalDataSharedPath.GetChildDirectoryWithName("ExplorerExtension");
        private readonly IAbsoluteFilePath[] _filePaths =
            new[] {"SN.withSIX.Mini.Presentation.Shell.dll", "Newtonsoft.Json.dll", "SharpShell.dll"}.Select(
                x => Common.Paths.AppPath.GetChildFileWithName(x)).ToArray();
        private readonly IExplorerExtensionInstaller _installer;

        public ExtensionHandler(IDbContextLocator dbContextLocator, IExplorerExtensionInstaller installer)
            : base(dbContextLocator) {
            _installer = installer;
        }

        public async Task<Unit> Handle(InstallExtension request) {
            var ctx = DbContextLocator.GetSettingsContext();
            await
                _installer.UpgradeOrInstall(_destination, await ctx.GetSettings().ConfigureAwait(false), _filePaths)
                    .ConfigureAwait(false);
            return Unit.Value;
        }

        public async Task<Unit> Handle(RemoveExtension request) {
            var ctx = DbContextLocator.GetSettingsContext();
            await
                _installer.Uninstall(_destination, await ctx.GetSettings().ConfigureAwait(false), _filePaths)
                    .ConfigureAwait(false);
            return Unit.Value;
        }
    }
}