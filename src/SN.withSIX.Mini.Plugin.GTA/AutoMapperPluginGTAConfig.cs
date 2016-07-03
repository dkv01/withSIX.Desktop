﻿// <copyright company="SIX Networks GmbH" file="AutoMapperPluginGTAConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SN.withSIX.Mini.Plugin.GTA.ApiModels;
using SN.withSIX.Mini.Plugin.GTA.Models;

namespace SN.withSIX.Mini.Plugin.GTA
{
    public class AutoMapperPluginGTAConfig
    {
        public static void Setup(IProfileExpression cfg) {
            SetupApiModels(cfg);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<GTA4GameSettings, GTA4GameSettingsApiModel>();
            cfg.CreateMap<GTA4GameSettingsApiModel, GTA4GameSettings>();

            cfg.CreateMap<GTA5GameSettings, GTA5GameSettingsApiModel>();
            cfg.CreateMap<GTA5GameSettingsApiModel, GTA5GameSettings>();
        }
    }
}