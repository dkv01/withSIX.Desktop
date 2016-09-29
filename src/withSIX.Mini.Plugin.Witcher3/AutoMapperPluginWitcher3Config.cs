// <copyright company="SIX Networks GmbH" file="AutoMapperPluginWitcher3Config.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using withSIX.Mini.Plugin.Witcher3.ApiModels;
using withSIX.Mini.Plugin.Witcher3.Models;

namespace withSIX.Mini.Plugin.Witcher3
{
    public class AutoMapperPluginWitcher3Config : Profile
    {
        public AutoMapperPluginWitcher3Config() {
            SetupApiModels(this);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<Witcher3GameSettings, Witcher3GameSettingsApiModel>();
            cfg.CreateMap<Witcher3GameSettingsApiModel, Witcher3GameSettings>();
        }
    }
}