﻿// <copyright company="SIX Networks GmbH" file="AutoMapperProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Plugin.Starbound.ApiModels;
using withSIX.Mini.Plugin.Starbound.Models;

namespace withSIX.Mini.Plugin.Starbound
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() {
            CreateMap<StarboundGameSettings, StarboundGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<StarboundGameSettingsApiModel, StarboundGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
        }
    }
}