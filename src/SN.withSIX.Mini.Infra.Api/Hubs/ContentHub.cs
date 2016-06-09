// <copyright company="SIX Networks GmbH" file="ContentHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Main.Games;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    // TODO: Error reporting to the Website client?
    // TODO: Error reporting to the Desktop client?
    public class ContentHub : HubBase<IContentClientHub>
    {
        public Task<ClientContentInfo2> GetContent(Guid gameId) => RequestAsync(new GetContent(gameId));

        public Task SelectGame(SelectGame command) => RequestAsync(command);

        public Task PlayContent(PlayContent command) => RequestAsync(command);

        public Task PlayContents(PlayContents command) => RequestAsync(command);

        public Task SyncCollections(SyncCollections command) => RequestAsync(command);

        public Task LaunchGame(LaunchGame command) => RequestAsync(command);

        public Task LaunchContent(LaunchContent command) => RequestAsync(command);

        public Task LaunchContents(LaunchContents command) => RequestAsync(command);

        public Task InstallContent(InstallContent command) => RequestAsync(command);

        public Task UninstallContent(UninstallContent command) => RequestAsync(command);

        public Task InstallCollection(InstallCollection command) => RequestAsync(command);

        public Task DeleteCollection(DeleteCollection command) => RequestAsync(command);

        public Task RemoveRecent(RemoveRecent command) => RequestAsync(command);

        public Task ClearRecent(ClearRecent command) => RequestAsync(command);

        public Task InstallContents(InstallContents command) => RequestAsync(command);

        public Task Next(Guid actionId) => DispatchNextAction(actionId);

        public Task Abort(Pause command) => RequestAsync(command);

        public Task AbortAll() => RequestAsync(new CancelAll());

        public Task<string> PrepareFolder() => RequestAsync(new PrepareFolder());

        public Task<IAbsoluteDirectoryPath> GetUploadFolder(GetUploadFolder query) => RequestAsync(query);

        public Task<Guid> UploadFolder(UploadFolder command) => RequestAsync(command);

        public Task OpenFolder(OpenFolder command) => RequestAsync(command);
    }

    public interface IContentClientHub
    {
        Task ContentFavorited(Guid gameId, FavoriteContentModel favoriteContent);
        Task ContentUnfavorited(Guid gameId, Guid contentId);
        Task RecentItemAdded(Guid gameId, RecentContentModel recentItem);
        Task RecentItemUsed(Guid gameId, Guid recentItemId, DateTime playedAt);
        Task ContentInstalled(Guid gameId, InstalledContentModel installedContent);
        Task RecentItemRemoved(Guid id);
    }
}