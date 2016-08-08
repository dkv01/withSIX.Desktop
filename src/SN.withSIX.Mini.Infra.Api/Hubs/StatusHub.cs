// <copyright company="SIX Networks GmbH" file="StatusHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public class StatusHub : HubBase<IStatusClientHub>
    {
        public Task<ClientContentInfo> GetState(Guid gameId) => RequestAsync(new GetState(gameId));

        public Task<GamesApiModel> GetGames() => RequestAsync(new GetGames());

        public Task<HomeApiModel> GetHome() => RequestAsync(new GetHome());

        public Task<GameHomeApiModel> GetGameHome(Guid id) => RequestAsync(new GetGameHome(id));

        public Task<CollectionsApiModel> GetGameCollections(Guid id, int page = 1) => RequestAsync(new GetGameCollections(id, page));

        public Task<ModsApiModel> GetGameMods(Guid id, int page = 1) => RequestAsync(new GetGameMods(id, page));

        public Task<MissionsApiModel> GetGameMissions(Guid id, int page = 1) => RequestAsync(new GetGameMissions(id, page));
    }

    public class ContentStateChange
    {
        public Guid GameId { get; set; }
        public Dictionary<Guid, ContentStatus> States { get; set; }
    }

    // TODO: More advanced state handling events including if updates are available
    // the domain needs to be extended to support that kind of stuff.
    // for now handle uptodate as Installed state..

    public interface IStatusClientHub
    {
        Task LockedGame(Guid gameId, bool canAbort);
        Task UnlockedGame(Guid gameId);
        Task ContentStateChanged(ContentStateChange state);
        Task ContentStatusChanged(ContentStatus contentStatus);
        Task ActionUpdateNotification(ActionTabStateUpdate notification);
        Task LaunchedGame(Guid id);
        Task TerminatedGame(Guid id);
        Task Locked();
        Task Unlocked();
        Task ActionNotification(ActionNotification notification);
    }
}