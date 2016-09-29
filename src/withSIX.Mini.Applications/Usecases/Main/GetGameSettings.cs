// <copyright company="SIX Networks GmbH" file="GetGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Usecases.Main
{
    public class GetGameSettings : IAsyncQuery<GameSettings>, IHaveId<Guid>
    {
        public GetGameSettings(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class GetGameSettingsHandler : DbQueryBase, IAsyncRequestHandler<GetGameSettings, GameSettings>
    {
        readonly IGameSettingsViewModelFactory _factory;

        public GetGameSettingsHandler(IDbContextLocator dbContextLocator, IGameSettingsViewModelFactory factory)
            : base(dbContextLocator) {
            _factory = factory;
        }

        public async Task<GameSettings> Handle(GetGameSettings request) => new GameSettings {
            Settings = _factory.CreateApiModel(
                await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false))
        };
    }

    public class GameSettings
    {
        public object Settings { get; set; }
    }
}