// <copyright company="SIX Networks GmbH" file="BasicGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class BasicGame : Game, ILaunchWith<IBasicGameLauncher>
    {
        protected BasicGame(Guid id, GameSettings settings) : base(id, settings) {}

        protected override Task InstallImpl(IContentInstallationService installationService,
            IDownloadContentAction<IInstallableContent> content)
            => installationService.Install(GetInstallAction(content));

        protected virtual InstallContentAction GetInstallAction(
            IDownloadContentAction<IInstallableContent> action)
            => new InstallContentAction(action.Content, action.CancelToken) {
                RemoteInfo = RemoteInfo,
                Paths = ContentPaths,
                Game = this,
                Cleaning = ContentCleaning,
                Force = action.Force,
                HideLaunchAction = action.HideLaunchAction,
                Name = action.Name
            };

        protected void ProcessAddedAndRemovedContent(LocalContent[] newContent, IEnumerable<Content> removedContent) {
            if (newContent.Any())
                AddInstalledContent(newContent);
            RemoveInstalledContent(removedContent);
            RefreshCollections();
        }

        void RemoveInstalledContent(IEnumerable<Content> lc) {
            foreach (var c in lc)
                c.Uninstalled();
        }

        protected static bool ContentExists(IAbsoluteDirectoryPath dir) => dir.Exists && !dir.IsEmpty();

        protected override Task UninstallImpl(IContentInstallationService contentInstallation,
            IContentAction<IUninstallableContent> uninstallLocalContentAction)
            => contentInstallation.Uninstall(GetUninstallAction(uninstallLocalContentAction));

        UnInstallContentAction GetUninstallAction(IContentAction<IUninstallableContent> action)
            => new UnInstallContentAction(this, action.Content, action.CancelToken) {
                Paths = ContentPaths
            };

        protected override async Task<Process> LaunchImpl(IGameLauncherFactory factory,
            ILaunchContentAction<IContent> launchContentAction) {
            var launcher = factory.Create(this);
            return await (IsLaunchingSteamApp()
                ? LaunchWithSteam(launcher, GetStartupParameters())
                : LaunchNormal(launcher, GetStartupParameters())).ConfigureAwait(false);
        }

        protected async Task<Process> LaunchNormal(ILaunch launcher, IEnumerable<string> startupParameters)
            =>
                await
                    launcher.Launch(await GetDefaultLaunchInfo(startupParameters).ConfigureAwait(false))
                        .ConfigureAwait(false);

        protected async Task<Process> LaunchWithSteam(ILaunchWithSteam launcher, IEnumerable<string> startupParameters)
            =>
                await
                    launcher.Launch(await GetSteamLaunchInfo(startupParameters).ConfigureAwait(false))
                        .ConfigureAwait(false);

        IEnumerable<string> GetStartupParameters() => Settings.StartupParameters.Get();
        // TODO
        protected override async Task ScanForLocalContentImpl() {}

        protected static IEnumerable<IContentWithPackageName> GetPackagedContent(
            IEnumerable<IContentSpec<IInstallableContent>> content)
            => content.SelectMany(x => x.Content.GetRelatedContent(constraint: x.Constraint))
                .Select(x => x.Content).Distinct().OfType<IContentWithPackageName>();
    }
}