// <copyright company="SIX Networks GmbH" file="UninstallerSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models;
using withSIX.Api.Models.Content;
using withSIX.Core.Helpers;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Steam.Core.Services;
using withSIX.Sync.Core.Legacy.Status;
using withSIX.Sync.Core.Packages;
using withSIX.Sync.Core.Repositories;

namespace withSIX.Mini.Applications.Services
{
    public class UninstallerSession : IUninstallSession
    {
        readonly IUninstallContentAction2<IUninstallableContent> _action;
        private readonly ISteamHelperRunner _steamHelperRunner;
        PackageManager _pm;
        Repository _repository;

        public UninstallerSession(IUninstallContentAction2<IUninstallableContent> action,
            ISteamHelperRunner steamHelperRunner) {
            _action = action;
            _steamHelperRunner = steamHelperRunner;
        }

        public async Task Uninstall() {
            try {
                foreach (var c in _action.Content)
                    await c.Content.Uninstall(this, _action.CancelToken).ConfigureAwait(false);
            } finally {
                _action.Game.RefreshCollections();
            }
        }

        public async Task Uninstall<T>(T content) where T : Content, IContentWithPackageName {
            var finalState = content.GetState();
            await new ContentStatusChanged(content, ItemState.Uninstalling, 25).Raise().ConfigureAwait(false);
            try {
                await new ContentStatusChanged(content, ItemState.Uninstalling, 50).Raise().ConfigureAwait(false);

                if (content.GetSource(_action.Game).Publisher == Publisher.Steam) {
                    var s = CreateSteamSession(new Dictionary<ulong, ProgressLeaf> {
                        {
                            Convert.ToUInt64(content.GetSource(_action.Game).PublisherId),
                            new ProgressLeaf(content.PackageName)
                        }
                    });
                    await s.Uninstall(_action.CancelToken).ConfigureAwait(false);
                    _action.Game.Delete(content);
                } else {
                    _action.Game.Delete(content);
                    using (_repository = new Repository(GetRepositoryPath(), true)) {
                        _pm = new PackageManager(_repository, _action.Paths.Path,
                            new StatusRepo(_action.CancelToken), true);
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
                var contentSpecs = content.GetRelatedContent(constraint)
                    .Where(x => x.Content is IUninstallableContent && x.Content != content).ToArray();

                var packedContent = contentSpecs.Where(x => x.Content is IContentWithPackageName);
                var steamContent = packedContent.Where(
                        x => ((IContentWithPackageName) x.Content).GetSource(_action.Game).Publisher == Publisher.Steam)
                    .ToDictionary(x => x.Content, x => x.Constraint);

                foreach (var c in contentSpecs.Where(x => !steamContent.ContainsKey(x.Content))) {
                    cancelToken.ThrowIfCancellationRequested();
                    await c.Content.PreUninstall(this, cancelToken, true).ConfigureAwait(false);
                    await ((IUninstallableContent) c.Content).Uninstall(this, cancelToken, constraint)
                        .ConfigureAwait(false);
                }

                if (steamContent.Any())
                    await ProcessSteamContent(cancelToken, steamContent).ConfigureAwait(false);

                if (content.IsInstalled()) {
                    _action.Status.Collections.Uninstall.Add(content.Id);
                    content.Uninstalled();
                }
                finalState = ItemState.NotInstalled;
            } finally {
                await new ContentStatusChanged(content, finalState).Raise().ConfigureAwait(false);
            }
        }

        private async Task ProcessSteamContent(CancellationToken cancelToken, Dictionary<Content, string> steamContent) {
            foreach (var c in steamContent)
                await c.Key.PreUninstall(this, cancelToken, true).ConfigureAwait(false);
            var s =
                CreateSteamSession(
                    steamContent.ToDictionary(
                        x =>
                            Convert.ToUInt64(
                                ((IContentWithPackageName) x.Key).GetSource(_action.Game).PublisherId),
                        x => new ProgressLeaf(((IContentWithPackageName) x.Key).PackageName)));
            await s.Uninstall(_action.CancelToken).ConfigureAwait(false);
            foreach (var c in steamContent)
                c.Key.Uninstalled();
            //await WaitForUninstalled(steamContent).ConfigureAwait(false);
        }

        /*
        private async Task WaitForUninstalled(Dictionary<Content, string> steamContent) {
            using (var cts = new CancellationTokenSource()) {
                cts.CancelAfter(TimeSpan.FromSeconds(60));
                // TODO: Or would we better ping steam?
                await
                    Observable.Interval(TimeSpan.FromMilliseconds(500)) // apiScheduler
                        .TakeWhile(
                            _ =>
                                steamContent.Select(
                                    x => ((IContentWithPackageName) x.Key).GetSourceDirectory(_action.Game))
                                    .Any(x => x.Exists))
                        .ToTask(cts.Token)
                        .ConfigureAwait(false);
            }
        }
*/

        private InstallerSession.SteamExternalInstallerSession CreateSteamSession(
            Dictionary<ulong, ProgressLeaf> progressLeaves)
            =>
                new InstallerSession.SteamExternalInstallerSession(_action.Game.SteamInfo.AppId,
                    _action.Game.SteamDirectories.Workshop.ContentPath, progressLeaves, _steamHelperRunner);

        IAbsoluteDirectoryPath GetRepositoryPath()
            => _action.Paths.RepositoryPath.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory);
    }
}