// <copyright company="SIX Networks GmbH" file="ContentInstaller.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Services;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Core.Games.Services.ContentInstaller
{
    // TODO: Deal with Path Access permissions (Elevate and set access bits for the user etc? or elevate self?)

    // TODO: cleanup installed mods, or do we do that at the Launch instead ?.. .. would probably need more control by the game then?
    // Or we eventually create an installation service per game, and a factory, like we do with the game launcher

    // TODO: Should it be the domain that enforces the single action (per game) lock, or rather the app?? Same goes for the status reporting..

    // TODO: We actually would like to support multi-installation capabilities per game, but let's start without?
    // Currently however we create a Repo object per session and therefore are locked..

    public static class StatusExtensions
    {
        public static bool IsEmpty(this InstallStatusOverview overview)
            => overview.Collections.IsEmpty() && overview.Mods.IsEmpty() && overview.Missions.IsEmpty();

        public static bool IsEmpty(this InstallStatus status)
            => !status.Install.Any() && !status.Uninstall.Any() && !status.Update.Any();
    }

    public class ContentInstaller : IContentInstallationService, IDomainService
    {
        public static readonly string SyncBackupDir = @".sync-backup";
        readonly ContentCleaner _cleaner;
        readonly Func<StatusChanged, Task> _eventRaiser;
        readonly IGameLocker _gameLocker;
        readonly IINstallerSessionFactory _sessionFactory;

        public ContentInstaller(Func<StatusChanged, Task> eventRaiser, IGameLocker gameLocker,
            IINstallerSessionFactory sessionFactory) {
            _eventRaiser = eventRaiser;
            _sessionFactory = sessionFactory;
            _gameLocker = gameLocker;
            _cleaner = new ContentCleaner();
        }

        public Task Abort(Guid gameId) => _gameLocker.Cancel(gameId);

        public Task Abort() => _gameLocker.Cancel();

        public async Task Uninstall(IUninstallContentAction2<IUninstallableContent> action) {
            await TryUninstall(action).ConfigureAwait(false);
        }

        public async Task Install(IInstallContentAction<IInstallableContent> action) {
            await TryInstall(action).ConfigureAwait(false);
        }

        async Task TryUninstall(IUninstallContentAction2<IUninstallableContent> action) {
            var session = _sessionFactory.CreateUninstaller(action);
            await session.Uninstall().ConfigureAwait(false);

            if (!action.Status.IsEmpty())
                await PostInstallStatusOverview(action.Status).ConfigureAwait(false);
        }

        async Task TryInstall(IInstallContentAction<IInstallableContent> action) {
            if (action.Cleaning.ShouldClean)
                await Clean(action).ConfigureAwait(false);
            await Synchronize(action).ConfigureAwait(false);
        }

        // TODO: .Concat exclusions based on Package info from each Content, so that we don't reinstall the desired content?
        Task Clean(IInstallContentAction<IInstallableContent> action)
            => _cleaner.CleanAsync(action.Paths.Path, action.Cleaning.Exclusions
                .Concat(
                    new IRelativePath[]
                    {@".\.synq".ToRelativeDirectoryPath(), @".\.sync-backup".ToRelativeDirectoryPath()})
                .ToArray(), action.Cleaning.FileTypes, action.Paths.Path.GetChildDirectoryWithName(SyncBackupDir));

        async Task Synchronize(IInstallContentAction<IInstallableContent> action) {
            try {
                await (await CreateSession(action).ConfigureAwait(false)).Synchronize().ConfigureAwait(false);
            } finally {
                if (!action.Status.IsEmpty())
                    await PostInstallStatusOverview(action.Status).ConfigureAwait(false);
            }
        }

        async Task<IInstallerSession> CreateSession(
            IInstallContentAction<IInstallableContent> action)
            => _sessionFactory.Create(action, info => StatusChange(Status.Synchronizing, info));

        // TODO: Post this to an async Queue that processes and retries in the background instead? (and perhaps merges queued items etc??)
        // And make the errors non fatal..
        static Task<HttpResponseMessage> PostInstallStatusOverview(InstallStatusOverview statusOverview)
            => Tools.Transfer.PostJson(statusOverview, new Uri(CommonUrls.SocialApiUrl, "/api/stats"));

        Task StatusChange(Status status, ProgressInfo info) => _eventRaiser(new StatusChanged(status, info));

        // NOTE: Stateful service!

        // TODO: Consider improving
        // - Keep a cache per app start?
        // - Keep a persistent cache?
        // - Further optimizations?
        // - Allow user to clean once, and then just base stuff on packages?
        class ContentCleaner
        {
            public bool BackupFiles { get; } = true;

            public Task CleanAsync(IAbsoluteDirectoryPath workingDirectory,
                IReadOnlyCollection<IRelativePath> exclusions, IEnumerable<string> fileTypes,
                IAbsoluteDirectoryPath backupPath)
                => TaskExtExt.StartLongRunningTask(() => Clean(workingDirectory, exclusions, fileTypes, backupPath));

            public void Clean(IAbsoluteDirectoryPath workingDirectory, IReadOnlyCollection<IRelativePath> exclusions,
                IEnumerable<string> fileTypes, IAbsoluteDirectoryPath backupPath) {
                Tools.FileUtil.Ops.CreateDirectoryAndSetACLWithFallbackAndRetry(workingDirectory);
                Tools.FileUtil.Ops.CreateDirectoryAndSetACLWithFallbackAndRetry(backupPath);
                var toRemove = GetFilesToRemove(workingDirectory, exclusions, fileTypes);
                foreach (var entry in toRemove) {
                    if (BackupFiles)
                        BackupEntry(workingDirectory, backupPath, entry);
                    else
                        entry.Delete();
                    // TODO: Optimize
                    DeleteEmptyFolders(entry.ParentDirectoryPath);
                }
            }

            static void BackupEntry(IAbsoluteDirectoryPath workingDirectory, IAbsoluteDirectoryPath backupPath,
                IAbsoluteFilePath entry) {
                var backupDestination = entry.GetRelativePathFrom(workingDirectory).GetAbsolutePathFrom(backupPath);
                DeleteDestinationIfDirectory(backupDestination);
                DeleteParentFilesIfExists(backupDestination, backupPath);
                backupDestination.MakeSureParentPathExists();
                entry.Move(backupDestination);
            }

            static void DeleteDestinationIfDirectory(IAbsoluteFilePath backupDestination) {
                if (Directory.Exists(backupDestination.ToString()))
                    backupDestination.ToString().ToAbsoluteDirectoryPath().Delete(true);
            }

            static void DeleteParentFilesIfExists(IPath path, IAbsoluteDirectoryPath backupPath) {
                while (path.HasParentDirectory) {
                    path = path.ParentDirectoryPath;
                    var s = path.ToString();
                    if (File.Exists(s)) {
                        File.Delete(s);
                        break;
                    }
                    if (path.Equals(backupPath))
                        break;
                }
            }

            static void DeleteEmptyFolders(IAbsoluteDirectoryPath path) {
                if (!path.DirectoryInfo.EnumerateFiles().Any()
                    && !path.DirectoryInfo.EnumerateDirectories().Any())
                    path.DirectoryInfo.Delete();
                while (path.HasParentDirectory) {
                    path = path.ParentDirectoryPath;
                    if (!path.DirectoryInfo.EnumerateFiles().Any()
                        && !path.DirectoryInfo.EnumerateDirectories().Any())
                        path.DirectoryInfo.Delete();
                }
            }

            static IEnumerable<IAbsoluteFilePath> GetFilesToRemove(IAbsoluteDirectoryPath workingDirectory,
                IReadOnlyCollection<IRelativePath> exclusions,
                IEnumerable<string> fileTypes) {
                var excludedDirectories = GetExcludedDirectories(workingDirectory, exclusions).ToArray();
                var excludedFiles = GetExcludedFiles(workingDirectory, exclusions).ToArray();
                return workingDirectory.GetFiles(fileTypes, SearchOption.AllDirectories)
                    .Where(x => IsNotExcluded(x, excludedDirectories, excludedFiles));
            }

            static bool IsNotExcluded(IFilePath x, IReadOnlyCollection<IAbsoluteDirectoryPath> excludedDirectories,
                IEnumerable<IAbsoluteFilePath> excludedFiles)
                => IsNotDirectoryExcluded(x, excludedDirectories) && !excludedFiles.Contains(x);

            static bool IsNotDirectoryExcluded(IFilePath x,
                IReadOnlyCollection<IAbsoluteDirectoryPath> excludedDirectories) {
                IPath b = x;
                while (b.HasParentDirectory) {
                    var parent = b.ParentDirectoryPath;
                    if (excludedDirectories.Contains(parent))
                        return false;
                    b = parent;
                }
                return true;
            }

            static IEnumerable<IAbsoluteFilePath> GetExcludedFiles(IAbsoluteDirectoryPath workingDirectory,
                IEnumerable<IRelativePath> exclusions) => exclusions.Where(x => x.IsFilePath)
                    .Select(x => x.GetAbsolutePathFrom(workingDirectory))
                    .Cast<IAbsoluteFilePath>();

            static IEnumerable<IAbsoluteDirectoryPath> GetExcludedDirectories(IAbsoluteDirectoryPath workingDirectory,
                IEnumerable<IRelativePath> exclusions) => exclusions.Where(x => x.IsDirectoryPath)
                    .Select(x => x.GetAbsolutePathFrom(workingDirectory))
                    .Cast<IAbsoluteDirectoryPath>();
        }
    }

    public interface IContentInstallationService
    {
        Task Install(IInstallContentAction<IInstallableContent> action);
        Task Abort(Guid gameId);
        Task Abort();
        Task Uninstall(IUninstallContentAction2<IUninstallableContent> action);
    }

    public class StatusChanged : ISyncDomainEvent
    {
        public StatusChanged(Status status, ProgressInfo info) {
            Contract.Requires<ArgumentNullException>(info != null);
            Status = status;
            Info = info;
        }

        public ProgressInfo Info { get; }

        public Status Status { get; }
    }

    public class ContentStatusChanged : ISyncDomainEvent
    {
        public ContentStatusChanged(IContent content, ItemState state, double progress = 0, long? speed = null) {
            if (progress.Equals(double.NaN))
                throw new ArgumentOutOfRangeException(nameof(progress), "NaN");
            if (progress < 0)
                throw new ArgumentOutOfRangeException(nameof(progress), "Below 0");
            if (speed < 0)
                throw new ArgumentOutOfRangeException(nameof(speed), "Below 0");
            Content = content;
            State = state;
            Progress = progress;
            Speed = speed;
        }

        public IContent Content { get; }
        public ItemState State { get; }
        public double Progress { get; }
        public long? Speed { get; }
    }

    public enum ItemState
    {
        NotInstalled = 0,

        Uptodate = 1, // We are installed @ latest version

        UpdateAvailable = 2, // We are installed, but dont have the latest version
        Incomplete = 3, // We tried starting install from scratch, but never finished.

        // Realtime action states..
        Installing = 11,
        Updating = 12,
        Uninstalling = 13,
        Diagnosing = 14,
        Launching = 15
    }

    public static class ItemStateExtensions
    {
        public static bool RequiresAction(this ItemState state)
            => state == ItemState.NotInstalled || state == ItemState.UpdateAvailable || state == ItemState.Incomplete;

        public static bool IsBusy(this ItemState state) => state >= ItemState.Installing;
    }

    public class AlreadyLockedException : Exception {}
}