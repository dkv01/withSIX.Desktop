// <copyright company="SIX Networks GmbH" file="StatusHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Hubs;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class StatusHub : HubBase<IStatusClientHub>
    {
        public Task<ClientContentInfo> GetState(Guid gameId) => Send(new GetState(gameId));

        public Task<GamesApiModel> GetGames() => Send(new GetGames());

        public Task<HomeApiModel> GetHome() => Send(new GetHome());

        public Task<GameHomeApiModel> GetGameHome(Guid id) => Send(new GetGameHome(id));

        public Task<CollectionsApiModel> GetGameCollections(GetGameCollections request) => Send(request);

        public Task<ModsApiModel> GetGameMods(GetGameMods request) => Send(request);

        public Task<MissionsApiModel> GetGameMissions(GetGameMissions request) => Send(request);
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