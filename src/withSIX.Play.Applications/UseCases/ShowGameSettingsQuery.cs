// <copyright company="SIX Networks GmbH" file="ShowGameSettingsQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel.Composition;
using MediatR;

using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Play.Applications.DataModels.Games;
using SN.withSIX.Play.Applications.Services.Infrastructure;
using SN.withSIX.Play.Applications.ViewModels.Games.Overlays;
using SN.withSIX.Play.Core.Games.Entities;
using SN.withSIX.Play.Core.Games.Entities.Other;
using SN.withSIX.Play.Core.Games.Entities.RealVirtuality;

namespace SN.withSIX.Play.Applications.UseCases
{
    public class ShowGameSettingsQuery : IRequest<GameSettingsOverlayViewModel>
    {
        public ShowGameSettingsQuery(Guid id) {
            ID = id;
        }

        public Guid ID { get; }
    }

    
    public class ShowGameSettingsQueryHandler : IRequestHandler<ShowGameSettingsQuery, GameSettingsOverlayViewModel>
    {
        readonly IGameContext _context;
        readonly ExportFactory<GameSettingsOverlayViewModel> _factory;
        readonly IGameMapperConfig _mapper;

        public ShowGameSettingsQueryHandler(IGameContext context, IGameMapperConfig mapper,
            ExportFactory<GameSettingsOverlayViewModel> factory) {
            _context = context;
            _mapper = mapper;
            _factory = factory;
        }

        public GameSettingsOverlayViewModel Handle(ShowGameSettingsQuery request) {
            var vm = _factory.CreateExport();
            vm.Value.GameSettings = Map(_context.Games.Find(request.ID));
            return vm.Value;
        }

        GameSettingsDataModel Map(Game game) {
            var mapped = MapSettings((dynamic) game.Settings);
            mapped.GameId = game.Id;
            return mapped;
        }

        // TODO: Generate these methods more dynamically...
        
        GameSettingsDataModel MapSettings(GameSettings settings) => _mapper.Map<GameSettingsDataModel>(settings);

        
        RealVirtualityGameSettingsDataModel MapSettings(RealVirtualitySettings settings) => _mapper.Map<RealVirtualityGameSettingsDataModel>(settings);

        
        ArmaSettingsDataModel MapSettings(ArmaSettings settings) => _mapper.Map<ArmaSettingsDataModel>(settings);

        
        Arma2OaSettingsDataModel MapSettings(Arma2OaSettings settings) => _mapper.Map<Arma2OaSettingsDataModel>(settings);

        
        Arma2CoSettingsDataModel MapSettings(Arma2CoSettings settings) => _mapper.Map<Arma2CoSettingsDataModel>(settings);

        
        Arma3SettingsDataModel MapSettings(Arma3Settings settings) => _mapper.Map<Arma3SettingsDataModel>(settings);

        
        HomeWorld2SettingsDataModel MapSettings(Homeworld2Settings settings) => _mapper.Map<HomeWorld2SettingsDataModel>(settings);

        
        GTAVSettingsDataModel MapSettings(GTAVSettings settings) => _mapper.Map<GTAVSettingsDataModel>(settings);
    }
}