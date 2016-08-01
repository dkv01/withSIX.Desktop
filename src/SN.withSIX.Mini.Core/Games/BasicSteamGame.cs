using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class BasicSteamGame : BasicGame
    {
        protected BasicSteamGame(Guid id, GameSettings settings) : base(id, settings) {}

        protected override IAbsoluteDirectoryPath GetContentDirectory() => GetSteamWorkshopFolder();

        private IAbsoluteDirectoryPath GetSteamWorkshopFolder()
            => Common.Paths.SteamPath.GetChildDirectoryWithName($@"steamapps\workshop\content\{SteamInfo.AppId}");

        // TODO: Use for temp downloading until complete (should we first copy data here then for chunk patching?)
        private IAbsoluteDirectoryPath GetSteamWorkshopDownloadFolder()
            => Common.Paths.SteamPath.GetChildDirectoryWithName($@"steamapps\workshop\downloads\{SteamInfo.AppId}");

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

        protected virtual async Task InstallMod(IModContent mod) {}
    }
}