// <copyright company="SIX Networks GmbH" file="BasicSteamGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class BasicSteamGame : BasicGame
    {
        protected BasicSteamGame(Guid id, GameSettings settings) : base(id, settings) {}

        protected override IAbsoluteDirectoryPath GetContentDirectory()
            => InstalledState.Directory.GetChildDirectoryWithName(".synqmods");

        protected override Task InstallImpl(IContentInstallationService installationService,
            IDownloadContentAction<IInstallableContent> action) {
            foreach (var m in GetPackagedContent(action.Content).OfType<ModNetworkContent>()) {
                m.RegisterAdditionalPostInstallTask(async processed => {
                    if (processed)
                        await InstallMod(m).ConfigureAwait(false);
                });
            }
            return base.InstallImpl(installationService, action);
        }

        protected override Task UninstallImpl(IContentInstallationService installationService,
            IContentAction<IUninstallableContent> action) {
            foreach (var m in action.Content.OfType<ModNetworkContent>()) {
                m.RegisterAdditionalPreUninstallTask(async processed => {
                    if (processed)
                        await UninstallMod(m).ConfigureAwait(false);
                });
            }
            return base.UninstallImpl(installationService, action);
        }

        protected abstract Task InstallMod(IModContent mod);
        protected abstract Task UninstallMod(IModContent mod);

        protected override async Task BeforeLaunch(ILaunchContentAction<IContent> action) {
            await EnableMods(action).ConfigureAwait(false);
            await base.BeforeLaunch(action).ConfigureAwait(false);
        }

        protected abstract Task EnableMods(ILaunchContentAction<IContent> launchContentAction);

        protected override Task ScanForLocalContentImpl()
            => TaskExt.StartLongRunningTask(() => ScanForLocalContentInternal());

        void ScanForLocalContentInternal() {
            var existingModFolders = GetExistingModFolders().ToArray();
            var newContent =
                new SteamGameContentScanner(this).ScanForNewContent(existingModFolders).ToArray();
            var removedContent = InstalledContent.OfType<IPackagedContent>()
                .Where(ContentExists)
                .Cast<Content>();
            ProcessAddedAndRemovedContent(newContent, removedContent);
        }

        private bool ContentExists(IPackagedContent x)
            =>
            ((x.GetSource(this).Publisher == Publisher.Steam) && !SteamDirectories.IsValid) ||
            !ContentExists(x.GetSourceDirectory(this));

        IEnumerable<IAbsoluteDirectoryPath> GetExistingModFolders() => GetModFolders().Where(x => x.Exists);

        protected virtual IEnumerable<IAbsoluteDirectoryPath> GetModFolders() {
            if (ContentPaths.IsValid)
                yield return ContentPaths.Path;
            if (SteamDirectories.IsValid && SteamDirectories.Workshop.ContentPath.Exists)
                yield return SteamDirectories.Workshop.ContentPath;
        }


        protected Task EnableModsInternal<T>(IEnumerable<T> content, Func<T, Task> act)
            => content.RunAndThrow(async m => {
                try {
                    await act(m).ConfigureAwait(false);
                } catch (NotInstallableException ex) {
                    MainLog.Logger.Warn($"Error enabling mod {ex}");
                }
            });

        protected static IAbsoluteFilePath GetBackupFile(IAbsoluteFilePath destPakFile)
            => destPakFile.GetBrotherFileWithName(destPakFile.FileNameWithoutExtension + ".bak");

        protected abstract class SteamMod
        {
            protected readonly IModContent Mod;
            protected readonly IAbsoluteDirectoryPath SourcePath;

            protected SteamMod(IAbsoluteDirectoryPath sourcePath, IModContent mod) {
                SourcePath = sourcePath;
                Mod = mod;
            }

            public async Task Install(bool force) {
                if (!SourcePath.Exists)
                    throw new NotFoundException($"{Mod.PackageName} source not found! You might try Diagnosing");
                await InstallImpl(force).ConfigureAwait(false);
            }

            protected abstract Task InstallImpl(bool force);

            public Task Uninstall() => UninstallImpl();
            protected abstract Task UninstallImpl();
        }
    }

    internal class SteamGameContentScanner
    {
        private readonly BasicSteamGame _game;

        public SteamGameContentScanner(BasicSteamGame game) {
            _game = game;
        }

        public IEnumerable<LocalContent> ScanForNewContent(IAbsoluteDirectoryPath[] existingModFolders) {
            foreach (var em in existingModFolders
                .SelectMany(d => d.ChildrenDirectoriesPath
                    .Where(x => !x.IsEmptySafe())
                    .Select(m => _game.Contents.OfType<IPackagedContent>()
                        .FirstOrDefault(x => x.PackageName == m.DirectoryName))
                    .Where(em => em != null)
                    .Where(em => em.InstallInfo == null)))
                em.Installed(null, false);
            return new List<LocalContent>();
        }
    }
}