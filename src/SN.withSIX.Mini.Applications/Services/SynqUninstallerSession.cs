// <copyright company="SIX Networks GmbH" file="SynqUninstallerSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Sync.Core.Packages;
using SN.withSIX.Sync.Core.Repositories;
using withSIX.Api.Models;

namespace SN.withSIX.Mini.Applications.Services
{
    public class SynqUninstallerSession : IUninstallSession
    {
        readonly IUninstallContentAction2<IUninstallableContent> _action;
        PackageManager _pm;
        Repository _repository;

        public SynqUninstallerSession(IUninstallContentAction2<IUninstallableContent> action) {
            _action = action;
        }

        public async Task Uninstall<T>(T content) where T : Content, IHavePackageName {
            var finalState = content.GetState();
            await new ContentStatusChanged(content, ItemState.Uninstalling, 25).Raise().ConfigureAwait(false);
            try {
                var dir = _action.Paths.Path.GetChildDirectoryWithName(content.PackageName);
                if (dir.Exists)
                    dir.DirectoryInfo.Delete(true);

                await new ContentStatusChanged(content, ItemState.Uninstalling, 50).Raise().ConfigureAwait(false);

                using (_repository = new Repository(GetRepositoryPath(), true)) {
                    _pm = new PackageManager(_repository, _action.Paths.Path, true);
                    _pm.DeletePackageIfExists(new SpecificVersion(content.PackageName));
                }

                if (content.IsInstalled()) {
                    if (content is IModContent)
                        _action.Status.Mods.Uninstall.Add(content.Id);
                    else if (content is MissionLocalContent)
                        _action.Status.Missions.Uninstall.Add(content.Id);
                    //else if (content is ICollectionContent)
                    //  _action.Status.Collections.Uninstall.Add(content.Id);
                    content.Uninstalled();
                    _action.Game.RefreshCollections();
                }
                finalState = ItemState.NotInstalled;
            } finally {
                await new ContentStatusChanged(content, finalState).Raise().ConfigureAwait(false);
            }
        }

        public async Task UninstallCollection(Collection content, CancellationToken cancelToken,
            string constraint = null) {
            var finalState = content.GetState();
            await new ContentStatusChanged(content, ItemState.Uninstalling, 25).Raise().ConfigureAwait(false);
            try {
                foreach (
                    var c in
                        content.GetRelatedContent(constraint: constraint)
                            .Where(x => x.Content is IUninstallableContent && x.Content != content)) {
                    cancelToken.ThrowIfCancellationRequested();
                    await
                        ((IUninstallableContent) c.Content).Uninstall(this, cancelToken, constraint)
                            .ConfigureAwait(false);
                }
                if (content.IsInstalled()) {
                    _action.Status.Collections.Uninstall.Add(content.Id);
                    content.Uninstalled();
                }
                finalState = ItemState.NotInstalled;
            } finally {
                await new ContentStatusChanged(content, finalState).Raise().ConfigureAwait(false);
            }
        }

        IAbsoluteDirectoryPath GetRepositoryPath()
            => _action.Paths.RepositoryPath.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory);
    }
}