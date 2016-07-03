// <copyright company="SIX Networks GmbH" file="AutoMapperPluginWitcher3Config.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SN.withSIX.Mini.Plugin.Witcher3.ApiModels;
using SN.withSIX.Mini.Plugin.Witcher3.Models;

namespace SN.withSIX.Mini.Plugin.Witcher3
{
    public class AutoMapperPluginWitcher3Config
    {
        public static void Setup(IProfileExpression cfg) {
            SetupApiModels(cfg);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<Witcher3GameSettings, Witcher3GameSettingsApiModel>();
            cfg.CreateMap<Witcher3GameSettingsApiModel, Witcher3GameSettings>();
        }
    }
}