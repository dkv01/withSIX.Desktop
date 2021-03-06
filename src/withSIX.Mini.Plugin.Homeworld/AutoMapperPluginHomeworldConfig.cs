﻿// <copyright company="SIX Networks GmbH" file="AutoMapperPluginHomeworldConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Plugin.Homeworld.ApiModels;
using withSIX.Mini.Plugin.Homeworld.Models;

namespace withSIX.Mini.Plugin.Homeworld
{
    public class AutoMapperPluginHomeworldConfig : Profile
    {
        public AutoMapperPluginHomeworldConfig() {
            SetupApiModels(this);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<Homeworld2GameSettings, Homeworld2GameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            cfg.CreateMap<Homeworld2GameSettingsApiModel, Homeworld2GameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
        }
    }
}