// <copyright company="SIX Networks GmbH" file="AutoMapperProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Plugin.CE.ApiModels;
using withSIX.Mini.Plugin.CE.Models;

namespace withSIX.Mini.Plugin.CE
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() {
            CreateMap<SkyrimGameSettings, SkyrimGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<SkyrimGameSettingsApiModel, SkyrimGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
            CreateMap<Fallout4GameSettings, Fallout4GameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<Fallout4GameSettingsApiModel, Fallout4GameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
        }
    }
}