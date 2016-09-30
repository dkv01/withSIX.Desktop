// <copyright company="SIX Networks GmbH" file="GameMessageHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using withSIX.Play.Applications.Services;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Applications.NotificationHandlers
{
    
    public class GameMessageHandler : INotificationHandler<ModPathChangedEvent>,
        INotificationHandler<ModAndSynqPathsChangedEvent>, INotificationHandler<SynqPathChangedEvent>
    {
        readonly IDialogManager _dialogManager;
        readonly UpdateManager _updateManager;

        public GameMessageHandler(UpdateManager updateManager, IDialogManager dialogManager) {
            _updateManager = updateManager;
            _dialogManager = dialogManager;
        }

        public async void Handle(ModAndSynqPathsChangedEvent notification) {
            var newModPath = notification.NewPaths.Path;
            if (newModPath != null)
                await ModPathWarningDialog(newModPath).ConfigureAwait(false);

            try {
                var oldSp = notification.OldPaths.RepositoryPath;
                var newSp = notification.NewPaths.RepositoryPath;
                await
                    MoveExistingModDataWhenRequested(notification.OldPaths.Path, notification.NewPaths.Path,
                        oldSp == null ? null : oldSp.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory),
                        newSp == null ? null : newSp.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory),
                        notification.Game)
                        .ConfigureAwait(false);
            } finally {
                ((Game) notification.Game).RefreshState();
            }
        }

        public async void Handle(ModPathChangedEvent notification) {
            var newModPath = notification.NewPaths.Path;
            if (newModPath != null)
                await ModPathWarningDialog(newModPath).ConfigureAwait(false);

            try {
                await
                    MoveExistingModDataWhenRequested(notification.OldPaths.Path, notification.NewPaths.Path, null, null,
                        notification.Game)
                        .ConfigureAwait(false);
            } finally {
                ((Game) notification.Game).RefreshState();
            }
        }

        public async void Handle(SynqPathChangedEvent notification) {
            var newSynqPath = notification.NewPaths.RepositoryPath;
            if (newSynqPath != null)
                await SynqPathWarningDialog(newSynqPath).ConfigureAwait(false);

            try {
                var oldSp = notification.OldPaths.RepositoryPath;
                var newSp = notification.NewPaths.RepositoryPath;
                await
                    MoveExistingSynqDataWhenRequested(
                        oldSp == null ? null : oldSp.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory),
                        newSp == null ? null : newSp.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory),
                        notification.Game)
                        .ConfigureAwait(false);
            } finally {
                ((Game) notification.Game).RefreshState();
            }
        }

        async Task MoveExistingModDataWhenRequested(IAbsoluteDirectoryPath oldModsPath,
            IAbsoluteDirectoryPath newModPath, IAbsoluteDirectoryPath oldSynqPath,
            IAbsoluteDirectoryPath newSynqPath, ISupportModding game) {
            if (ArePathsEqual(oldModsPath, newModPath)) {
                if (oldSynqPath != null && newSynqPath != null)
                    await MoveExistingSynqDataWhenRequested(oldSynqPath, newSynqPath, game);
                return;
            }

            var todo = await ModPathChangedDialog(oldModsPath, newModPath);
            if (todo) {
                await
                    ErrorHandlerr.TryAction(
                        () => PerformMoveModAndSynq(oldModsPath, newModPath, oldSynqPath, newSynqPath, game))
                        .ConfigureAwait(false);
            }
        }

        async Task PerformMoveModAndSynq(IAbsoluteDirectoryPath oldModsPath, IAbsoluteDirectoryPath newModPath,
            IAbsoluteDirectoryPath oldSynqPath, IAbsoluteDirectoryPath newSynqPath, ISupportModding game) {
            await _updateManager.MoveModFoldersIfValidAndExists(oldModsPath, newModPath).ConfigureAwait(false);
            if (oldSynqPath != null && newSynqPath != null
                && !Tools.FileUtil.ComparePathsOsCaseSensitive(oldSynqPath.ToString(), newSynqPath.ToString())) {
                game.Controller.BundleManager.Repo.Dispose(); // Unlock
                await _updateManager.MovePathIfValidAndExists(oldSynqPath, newSynqPath);
            }
        }

        async Task MoveExistingSynqDataWhenRequested(IAbsoluteDirectoryPath oldSynqPath,
            IAbsoluteDirectoryPath newSynqPath, ISupportModding game) {
            if (ArePathsEqual(oldSynqPath, newSynqPath))
                return;

            var todo = await SynqPathChangedDialog(oldSynqPath, newSynqPath).ConfigureAwait(false);

            if (todo) {
                game.Controller.BundleManager.Repo.Dispose(); // Unlock
                await
                    ErrorHandlerr.TryAction(
                        () => _updateManager.MovePathIfValidAndExists(oldSynqPath, newSynqPath))
                        .ConfigureAwait(false);
            }
        }

        static bool ArePathsEqual(IAbsoluteDirectoryPath old, IAbsoluteDirectoryPath @new) => old == @new ||
       (old != null && @new != null &&
        Tools.FileUtil.ComparePathsOsCaseSensitive(old.ToString(), @new.ToString()));

        async Task<bool> ModPathChangedDialog(IAbsoluteDirectoryPath oldModsPath, IAbsoluteDirectoryPath newModPath) => (await _dialogManager.MessageBox(new MessageBoxDialogParams(
            $"Mod path changed, do you want to move existing data?\nFrom: {oldModsPath} to {newModPath}", "Move existing data?", SixMessageBoxButton.YesNo)))
       == SixMessageBoxResult.Yes;

        async Task<bool> SynqPathChangedDialog(IAbsoluteDirectoryPath oldSynqPath, IAbsoluteDirectoryPath newSynqPath) => (await _dialogManager.MessageBox(new MessageBoxDialogParams(
            $"Synq path changed, do you want to move existing data?\nFrom: {oldSynqPath} to {newSynqPath}", "Move existing data?", SixMessageBoxButton.YesNo)))
       == SixMessageBoxResult.Yes;

        Task<SixMessageBoxResult> ModPathWarningDialog(IAbsoluteDirectoryPath value) => _dialogManager.MessageBox(new MessageBoxDialogParams(
            $"You seem to have set a custom Mod installation path: {value}\nPlease make sure this path is writable, or restart the application as Administrator if needed", "Custom path set"));

        Task<SixMessageBoxResult> SynqPathWarningDialog(IAbsoluteDirectoryPath value) => _dialogManager.MessageBox(new MessageBoxDialogParams(
            $"You seem to have set a custom synq installation path: {value}\nPlease make sure this path is writable, or restart the application as Administrator if needed", "Custom path set"));
    }
}