// <copyright company="SIX Networks GmbH" file="SaveGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Usecases.Main
{
    public class SaveGameSettings : IAsyncVoidCommand, IHaveId<Guid>, IHaveGameId
    {
        public GameSettingsApiModel Settings { get; set; }
        public Guid GameId => Id;
        public Guid Id { get; set; }
    }

    public class SaveGameSettingsHandler : DbCommandBase, IAsyncVoidCommandHandler<SaveGameSettings>
    {
        public SaveGameSettingsHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<Unit> Handle(SaveGameSettings request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var settings = game.Settings;
            request.Settings.MapTo(settings);
            await game.UpdateSettings(settings).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}