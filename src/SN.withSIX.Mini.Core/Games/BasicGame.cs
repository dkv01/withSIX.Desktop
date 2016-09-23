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
using SN.withSIX.Mini.Core.Extensions;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class BasicGame : Game, ILaunchWith<IBasicGameLauncher>
    {
        protected BasicGame(Guid id, GameSettings settings) : base(id, settings) {}

        protected override Task InstallImpl(IContentInstallationService installationService,
            IDownloadContentAction<IInstallableContent> action)
            => installationService.Install(GetInstallAction(action));

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
            IContentAction<IUninstallableContent> action)
            => contentInstallation.Uninstall(GetUninstallAction(action));

        UnInstallContentAction GetUninstallAction(IContentAction<IUninstallableContent> action)
            => new UnInstallContentAction(this, action.Content, action.CancelToken) {
                Paths = ContentPaths
            };

        protected override Task<Process> LaunchImpl(IGameLauncherFactory factory,
            ILaunchContentAction<IContent> action) {
            var launcher = factory.Create(this);
            return InitiateLaunch(launcher,
                new LaunchState(GetLaunchExecutable(action.Action), GetExecutable(action.Action), GetStartupParameters(action).ToArray(), action.Action));
        }

        protected Task<Process> InitiateLaunch<T>(T launcher, LaunchState ls)
            where T : ILaunch, ILaunchWithSteam => ShouldLaunchWithSteam(ls)
                ? LaunchWithSteam(launcher, ls)
                : LaunchNormal(launcher, ls);

        protected async Task<Process> LaunchNormal(ILaunch launcher, LaunchState ls)
            =>
                await
                    launcher.Launch(await GetDefaultLaunchInfo(ls).ConfigureAwait(false))
                        .ConfigureAwait(false);

        protected async Task<Process> LaunchWithSteam(ILaunchWithSteam launcher, LaunchState ls)
            =>
                await
                    launcher.Launch(await GetSteamLaunchInfo(ls).ConfigureAwait(false))
                        .ConfigureAwait(false);

        protected virtual IEnumerable<string> GetStartupParameters(ILaunchContentAction<IContent> action) => Settings.StartupParameters.Get();


        protected virtual IReadOnlyCollection<ILaunchableContent> GetLaunchables(ILaunchContentAction<IContent> action)
            => action.GetLaunchables().ToArray();

        // TODO
        protected override async Task ScanForLocalContentImpl() {}

        protected static IEnumerable<IContentWithPackageName> GetPackagedContent(
            IEnumerable<IContentSpec<IInstallableContent>> content)
            => content.SelectMany(x => x.Content.GetRelatedContent(x.Constraint))
                .Select(x => x.Content).Distinct().OfType<IContentWithPackageName>();
    }
}