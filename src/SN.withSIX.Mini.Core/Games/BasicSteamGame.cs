// <copyright company="SIX Networks GmbH" file="BasicSteamGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class BasicSteamGame : BasicGame
    {
        protected BasicSteamGame(Guid id, GameSettings settings) : base(id, settings) {}

        protected override IAbsoluteDirectoryPath GetContentDirectory()
            => InstalledState.WorkingDirectory.GetChildDirectoryWithName(".synqmods");

        protected override Task InstallImpl(IContentInstallationService installationService,
            IDownloadContentAction<IInstallableContent> content) {
            foreach (var m in GetPackagedContent(content.Content).OfType<ModNetworkContent>()) {
                m.RegisterAdditionalPostInstallTask(async processed => {
                    if (processed)
                        await InstallMod(m).ConfigureAwait(false);
                });
            }
            return base.InstallImpl(installationService, content);
        }

        protected override Task UninstallImpl(IContentInstallationService installationService,
            IContentAction<IUninstallableContent> uninstallLocalContentAction) {
            foreach (var m in uninstallLocalContentAction.Content.OfType<ModNetworkContent>()) {
                m.RegisterAdditionalPreUninstallTask(async processed => {
                    if (processed)
                        await UninstallMod(m).ConfigureAwait(false);
                });
            }
            return base.UninstallImpl(installationService, uninstallLocalContentAction);
        }

        protected abstract Task InstallMod(IModContent mod);
        protected abstract Task UninstallMod(IModContent mod);

        protected override async Task<Process> LaunchImpl(IGameLauncherFactory factory,
            ILaunchContentAction<IContent> launchContentAction) {
            await EnableMods(launchContentAction).ConfigureAwait(false);
            return await base.LaunchImpl(factory, launchContentAction).ConfigureAwait(false);
        }

        protected abstract Task EnableMods(ILaunchContentAction<IContent> launchContentAction);
    }
}