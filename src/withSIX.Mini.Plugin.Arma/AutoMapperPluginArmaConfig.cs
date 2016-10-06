// <copyright company="SIX Networks GmbH" file="AutoMapperPluginArmaConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using GameServerQuery.Games.RV;
using withSIX.Api.Models.Servers;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Plugin.Arma.ApiModels;
using withSIX.Mini.Plugin.Arma.Models;
using withSIX.Mini.Plugin.Arma.Services;

namespace withSIX.Mini.Plugin.Arma
{
    public class AutoMapperPluginArmaConfig : Profile
    {
        public AutoMapperPluginArmaConfig() {
            SetupApiModels();
            CreateMap<ArmaServerInfoModel, ServerInfo<ArmaServerInfoModel>>()
                .ForMember(x => x.Details, opt => opt.MapFrom(src => src))
                .ForMember(x => x.QueryAddress, opt => opt.MapFrom(src => src.QueryEndPoint))
                .ForMember(x => x.ConnectionAddress, opt => opt.MapFrom(src => src.ConnectionEndPoint));
            CreateMap<GameTags, Server>()
                .Include<GameTags, ArmaServer>();
            CreateMap<GameTags, ArmaServer>()
                .ForMember(x => x.IsLocked, opt => opt.MapFrom(src => src.Lock));
        }

        void SetupApiModels() {
            CreateMap<Arma2COGameSettings, Arma2COGameSettingsApiModel>();
            CreateMap<Arma2COGameSettingsApiModel, Arma2COGameSettings>();
            CreateMap<Arma3GameSettings, Arma3GameSettingsApiModel>();
            CreateMap<Arma3GameSettingsApiModel, Arma3GameSettings>();
            CreateMap<DayZGameSettings, DayZGameSettingsApiModel>();
            CreateMap<DayZGameSettingsApiModel, DayZGameSettings>();
            CreateMap<IronFrontGameSettings, IronFrontGameSettingsApiModel>();
            CreateMap<IronFrontGameSettingsApiModel, IronFrontGameSettings>();
            CreateMap<TakeOnHelicoptersGameSettings, TakeOnHelicoptersGameSettingsApiModel>();
            CreateMap<TakeOnHelicoptersGameSettingsApiModel, TakeOnHelicoptersGameSettings>();
            CreateMap<TakeOnMarsGameSettings, TakeOnMarsGameSettingsApiModel>();
            CreateMap<TakeOnMarsGameSettingsApiModel, TakeOnMarsGameSettings>();
            CreateMap<CarrierCommandGameSettings, CarrierCommandGameSettingsApiModel>();
            CreateMap<CarrierCommandGameSettingsApiModel, CarrierCommandGameSettings>();
        }
    }
}