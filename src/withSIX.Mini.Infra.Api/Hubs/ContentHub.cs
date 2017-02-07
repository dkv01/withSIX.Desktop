// <copyright company="SIX Networks GmbH" file="ContentHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Hubs;
using NDepend.Path;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Features.Main.Games;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class ContentHub : HubBase<IContentClientHub>
    {
        public Task<ClientContentInfo2> GetContent(Guid gameId) => Send(new GetContent(gameId));

        public Task SelectGame(SelectGame command) => Send(command);

        public Task CloseGame(CloseGame command) => Send(command);

        public Task PlayContent(PlayContent command) => Send(command);

        public Task PlayContents(PlayContents command) => Send(command);

        public Task SyncCollections(SyncCollections command) => Send(command);

        public Task LaunchGame(LaunchGame command) => Send(command);

        public Task LaunchContent(LaunchContent command) => Send(command);

        public Task LaunchContents(LaunchContents command) => Send(command);

        public Task InstallContent(InstallContent command) => Send(command);

        public Task UninstallContent(UninstallContent command) => Send(command);

        public Task InstallCollection(InstallCollection command) => Send(command);

        public Task DeleteCollection(DeleteCollection command) => Send(command);

        public Task RemoveRecent(RemoveRecent command) => Send(command);

        public Task ClearRecent(ClearRecent command) => Send(command);

        public Task InstallContents(InstallContents command) => Send(command);

        public Task InstallSteamContents(InstallSteamContents command) => Send(command);

        public Task Next(Guid actionId) => DispatchNextAction(actionId);

        public Task Abort(Pause command) => Send(command);

        public Task AbortAll() => Send(new CancelAll());

        public Task<string> PrepareFolder() => Send(new PrepareFolder());

        public Task<IAbsoluteDirectoryPath> GetUploadFolder(GetUploadFolder query) => Send(query);

        public Task<Guid> UploadFolder(UploadFolder command) => Send(command);

        public Task OpenFolder(OpenFolder command) => Send(command);
        public Task StartSession(StartDownloadSession command) => Send(command);
        public Task ScanLocalContent(Guid gameId) => Send(new ScanLocalContent(gameId));
        public Task AddModMapping(AddModMapping command) => Send(command);
    }

    public interface IContentClientHub
    {
        Task RecentItemAdded(Guid gameId, RecentContentModel recentItem);
        Task RecentItemUsed(Guid gameId, Guid recentItemId, DateTime playedAt);
        Task ContentInstalled(Guid gameId, InstalledContentModel installedContent);
        Task RecentItemRemoved(Guid id);
    }
}