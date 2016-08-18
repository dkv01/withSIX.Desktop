// <copyright company="SIX Networks GmbH" file="SynqUninstallerSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
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
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;

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

        public async Task Uninstall() {
            foreach (var c in _action.Content)
                await c.Content.Uninstall(this, _action.CancelToken).ConfigureAwait(false);
        }

        public async Task Uninstall<T>(T content) where T : Content, IContentWithPackageName {
            var finalState = content.GetState();
            await new ContentStatusChanged(content, ItemState.Uninstalling, 25).Raise().ConfigureAwait(false);
            try {
                var dir = content.GetSourceDirectory(_action.Game);
                if (dir.Exists)
                    dir.Delete(true);

                await new ContentStatusChanged(content, ItemState.Uninstalling, 50).Raise().ConfigureAwait(false);

                if (content.Source.Publisher == Publisher.Steam) {
                    var s = CreateSteamSession(new Dictionary<ulong, ProgressLeaf> {
                        {Convert.ToUInt64(content.Source.PublisherId), new ProgressLeaf(content.Name)}
                    });
                    await s.Uninstall(_action.CancelToken).ConfigureAwait(false);
                } else {
                    using (_repository = new Repository(GetRepositoryPath(), true)) {
                        _pm = new PackageManager(_repository, _action.Paths.Path, true);
                        _pm.DeletePackageIfExists(new SpecificVersion(content.PackageName));
                    }
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
                var contentSpecs = content.GetRelatedContent(constraint: constraint)
                    .Where(x => x.Content is IUninstallableContent && x.Content != content).ToArray();

                var packedContent = contentSpecs.Where(x => x.Content is IContentWithPackageName);
                var steamContent = packedContent.Where(x => ((IContentWithPackageName) x.Content).Source.Publisher == Publisher.Steam)
                        .ToDictionary(x => x.Content, x => x.Constraint);

                foreach (var c in contentSpecs.Where(x => !steamContent.ContainsKey(x.Content))) {
                    cancelToken.ThrowIfCancellationRequested();
                    await c.Content.PreUninstall(this, cancelToken, true).ConfigureAwait(false);
                    await ((IUninstallableContent) c.Content).Uninstall(this, cancelToken, constraint)
                        .ConfigureAwait(false);
                }

                foreach (var c in steamContent)
                    await c.Key.PreUninstall(this, cancelToken, true).ConfigureAwait(false);
                var s =
                    CreateSteamSession(
                        steamContent.ToDictionary(
                            x => Convert.ToUInt64(((IContentWithPackageName) x.Key).Source.PublisherId),
                            x => new ProgressLeaf(x.Key.Name)));
                await s.Uninstall(_action.CancelToken).ConfigureAwait(false);

                if (content.IsInstalled()) {
                    _action.Status.Collections.Uninstall.Add(content.Id);
                    content.Uninstalled();
                }
                finalState = ItemState.NotInstalled;
            } finally {
                await new ContentStatusChanged(content, finalState).Raise().ConfigureAwait(false);
            }
        }

        private SynqInstallerSession.SteamExternalInstallerSession CreateSteamSession(
            Dictionary<ulong, ProgressLeaf> progressLeaves)
            =>
                new SynqInstallerSession.SteamExternalInstallerSession(_action.Game.SteamInfo.AppId,
                    _action.Game.SteamworkshopPaths.ContentPath, progressLeaves);

        IAbsoluteDirectoryPath GetRepositoryPath()
            => _action.Paths.RepositoryPath.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory);
    }
}