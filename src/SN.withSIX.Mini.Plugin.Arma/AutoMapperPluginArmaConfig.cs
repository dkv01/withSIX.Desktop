// <copyright company="SIX Networks GmbH" file="AutoMapperPluginArmaConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SN.withSIX.Mini.Plugin.Arma.ApiModels;
using SN.withSIX.Mini.Plugin.Arma.Models;

namespace SN.withSIX.Mini.Plugin.Arma
{
    public class AutoMapperPluginArmaConfig : Profile
    {
        public AutoMapperPluginArmaConfig() {
            SetupApiModels(this);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<Arma2COGameSettings, Arma2COGameSettingsApiModel>();
            cfg.CreateMap<Arma2COGameSettingsApiModel, Arma2COGameSettings>();
            cfg.CreateMap<Arma3GameSettings, Arma3GameSettingsApiModel>();
            cfg.CreateMap<Arma3GameSettingsApiModel, Arma3GameSettings>();
            cfg.CreateMap<DayZGameSettings, DayZGameSettingsApiModel>();
            cfg.CreateMap<DayZGameSettingsApiModel, DayZGameSettings>();
            cfg.CreateMap<IronFrontGameSettings, IronFrontGameSettingsApiModel>();
            cfg.CreateMap<IronFrontGameSettingsApiModel, IronFrontGameSettings>();
            cfg.CreateMap<TakeOnHelicoptersGameSettings, TakeOnHelicoptersGameSettingsApiModel>();
            cfg.CreateMap<TakeOnHelicoptersGameSettingsApiModel, TakeOnHelicoptersGameSettings>();
            cfg.CreateMap<TakeOnMarsGameSettings, TakeOnMarsGameSettingsApiModel>();
            cfg.CreateMap<TakeOnMarsGameSettingsApiModel, TakeOnMarsGameSettings>();
            cfg.CreateMap<CarrierCommandGameSettings, CarrierCommandGameSettingsApiModel>();
            cfg.CreateMap<CarrierCommandGameSettingsApiModel, CarrierCommandGameSettings>();
        }
    }
}