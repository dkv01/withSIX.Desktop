// <copyright company="SIX Networks GmbH" file="ContentHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Hubs;
using NDepend.Path;
using withSIX.Mini.Applications.Usecases;
using withSIX.Mini.Applications.Usecases.Main;
using withSIX.Mini.Applications.Usecases.Main.Games;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class ContentHub : HubBase<IContentClientHub>
    {
        public Task<ClientContentInfo2> GetContent(Guid gameId) => SendAsync(new GetContent(gameId));

        public Task SelectGame(SelectGame command) => SendAsync(command);

        public Task CloseGame(CloseGame command) => SendAsync(command);

        public Task PlayContent(PlayContent command) => SendAsync(command);

        public Task PlayContents(PlayContents command) => SendAsync(command);

        public Task SyncCollections(SyncCollections command) => SendAsync(command);

        public Task LaunchGame(LaunchGame command) => SendAsync(command);

        public Task LaunchContent(LaunchContent command) => SendAsync(command);

        public Task LaunchContents(LaunchContents command) => SendAsync(command);

        public Task InstallContent(InstallContent command) => SendAsync(command);

        public Task UninstallContent(UninstallContent command) => SendAsync(command);

        public Task InstallCollection(InstallCollection command) => SendAsync(command);

        public Task DeleteCollection(DeleteCollection command) => SendAsync(command);

        public Task RemoveRecent(RemoveRecent command) => SendAsync(command);

        public Task ClearRecent(ClearRecent command) => SendAsync(command);

        public Task InstallContents(InstallContents command) => SendAsync(command);

        public Task InstallSteamContents(InstallSteamContents command) => SendAsync(command);

        public Task Next(Guid actionId) => DispatchNextAction(actionId);

        public Task Abort(Pause command) => SendAsync(command);

        public Task AbortAll() => SendAsync(new CancelAll());

        public Task<string> PrepareFolder() => SendAsync(new PrepareFolder());

        public Task<IAbsoluteDirectoryPath> GetUploadFolder(GetUploadFolder query) => SendAsync(query);

        public Task<Guid> UploadFolder(UploadFolder command) => SendAsync(command);

        public Task OpenFolder(OpenFolder command) => SendAsync(command);
        public Task StartSession(StartDownloadSession command) => SendAsync(command);
    }

    public interface IContentClientHub : IHub
    {
        Task RecentItemAdded(Guid gameId, RecentContentModel recentItem);
        Task RecentItemUsed(Guid gameId, Guid recentItemId, DateTime playedAt);
        Task ContentInstalled(Guid gameId, InstalledContentModel installedContent);
        Task RecentItemRemoved(Guid id);
    }
}