// <copyright company="SIX Networks GmbH" file="SaveGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MediatR;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class SaveGameSettings : IAsyncVoidCommand, IHaveId<Guid>, IHaveGameId
    {
        public object Settings { get; set; }
        public Guid GameId => Id;
        public Guid Id { get; set; }
    }

    public class SaveGameSettingsHandler : DbCommandBase, IAsyncVoidCommandHandler<SaveGameSettings>
    {
        public SaveGameSettingsHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<Unit> Handle(SaveGameSettings request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            // TODO: Specific game settings types..
            JsonConvert.PopulateObject(request.Settings.ToString(), game.Settings,
                JsonSupport.DefaultSettings);
            var startupLine = ((dynamic) request.Settings).startupLine;
            game.Settings.StartupParameters.StartupLine = startupLine;
            await game.UpdateSettings(game.Settings).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}