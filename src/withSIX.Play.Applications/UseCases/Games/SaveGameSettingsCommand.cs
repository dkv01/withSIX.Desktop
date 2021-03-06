﻿// <copyright company="SIX Networks GmbH" file="SaveGameSettingsCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using MediatR;

using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.DataModels.Games;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class SaveGameSettingsCommand : IRequest<Unit>
    {
        public GameSettingsDataModel Settings { get; set; }
    }

    
    public class SaveGameSettingsCommandHandler : IRequestHandler<SaveGameSettingsCommand, Unit>
    {
        readonly IGameContext _context;
        readonly IGameMapperConfig _gameMapper;

        public SaveGameSettingsCommandHandler(IGameContext context, IGameMapperConfig gameMapper) {
            _context = context;
            _gameMapper = gameMapper;
        }

        public Unit Handle(SaveGameSettingsCommand message) {
            var game = _context.Games.Find(message.Settings.GameId);

            ContentPaths modPaths = null;
            var modding = game as ISupportModding;
            if (modding != null)
                modPaths = modding.ModPaths;

            _gameMapper.Map(message.Settings, game.Settings);
            _gameMapper.Map(game.Settings, message.Settings);

            if (modding != null)
                HandleNewModPaths(modding, modding.ModPaths, modPaths);

            return Unit.Value;
        }

        // TODO: Domain events should be raised by the domain, not by the application layer,
        // however it is very difficult in the current setup to detect if modpaths were changed due to a profile change or due to a settings change...
        void HandleNewModPaths(ISupportModding game, ContentPaths newPaths, ContentPaths oldPaths) {
            if (!newPaths.IsValid || !oldPaths.IsValid)
                return;

            if (!oldPaths.EqualPath(newPaths)) {
                if (!oldPaths.EqualRepositoryPath(newPaths))
                    Cheat.PublishDomainEvent(new ModAndSynqPathsChangedEvent(game, oldPaths, newPaths));
                else
                    Cheat.PublishDomainEvent(new ModPathChangedEvent(game, oldPaths, newPaths));
            } else if (!oldPaths.EqualRepositoryPath(newPaths))
                Cheat.PublishDomainEvent(new SynqPathChangedEvent(game, oldPaths, newPaths));
        }
    }
}