// <copyright company="SIX Networks GmbH" file="SettingsHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Mini.Applications.Features.Main;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class SettingsHub : HubBase<ISettingsClientHub>
    {
        public Task<GeneralSettings> GetGeneral() => SendAsync(new GetGeneralSettings());

        public Task<GamesSettings> GetGames() => SendAsync(new GetGamesSettings());

        public Task<GameSettings> GetGame(Guid id) => SendAsync(new GetGameSettings(id));

        public Task SaveGeneralSettings(SaveGeneralSettings command) => SendAsync(command);

        public Task SaveGameSettings(SaveGameSettings command) => SendAsync(command);
    }

    public interface ISettingsClientHub {}
}