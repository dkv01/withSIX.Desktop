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
        public Task<ClientContentInfo> GetState(Guid gameId) => SendAsync(new GetState(gameId));

        public Task<GamesApiModel> GetGames() => SendAsync(new GetGames());

        public Task<HomeApiModel> GetHome() => SendAsync(new GetHome());

        public Task<GameHomeApiModel> GetGameHome(Guid id) => SendAsync(new GetGameHome(id));

        public Task<CollectionsApiModel> GetGameCollections(GetGameCollections request) => SendAsync(request);

        public Task<ModsApiModel> GetGameMods(GetGameMods request) => SendAsync(request);

        public Task<MissionsApiModel> GetGameMissions(GetGameMissions request) => SendAsync(request);
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