// <copyright company="SIX Networks GmbH" file="AutoMapperPluginHomeworldConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SN.withSIX.Mini.Plugin.Homeworld.ApiModels;
using SN.withSIX.Mini.Plugin.Homeworld.Models;

namespace SN.withSIX.Mini.Plugin.Homeworld
{
    public class AutoMapperPluginHomeworldConfig
    {
        public static void Setup(IProfileExpression cfg) {
            SetupApiModels(cfg);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<Homeworld2GameSettings, Homeworld2GameSettingsApiModel>();
            cfg.CreateMap<Homeworld2GameSettingsApiModel, Homeworld2GameSettings>();
        }
    }
}