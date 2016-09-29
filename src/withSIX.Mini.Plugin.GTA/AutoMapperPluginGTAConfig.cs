// <copyright company="SIX Networks GmbH" file="AutoMapperPluginGTAConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using withSIX.Mini.Plugin.GTA.ApiModels;
using withSIX.Mini.Plugin.GTA.Models;

namespace withSIX.Mini.Plugin.GTA
{
    public class AutoMapperPluginGTAConfig : Profile
    {
        public AutoMapperPluginGTAConfig() {
            SetupApiModels(this);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<GTA4GameSettings, GTA4GameSettingsApiModel>();
            cfg.CreateMap<GTA4GameSettingsApiModel, GTA4GameSettings>();

            cfg.CreateMap<GTA5GameSettings, GTA5GameSettingsApiModel>();
            cfg.CreateMap<GTA5GameSettingsApiModel, GTA5GameSettings>();
        }
    }
}