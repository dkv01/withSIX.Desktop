// <copyright company="SIX Networks GmbH" file="AutoMapperPluginKerbalConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using withSIX.Mini.Plugin.Kerbal.ApiModels;
using withSIX.Mini.Plugin.Kerbal.Models;

namespace withSIX.Mini.Plugin.Kerbal
{
    public class AutoMapperPluginKerbalConfig : Profile
    {
        public AutoMapperPluginKerbalConfig() {
            SetupApiModels(this);
        }

        static void SetupApiModels(IProfileExpression cfg) {
            cfg.CreateMap<KerbalSPGameSettings, KerbalSPGameSettingsApiModel>();
            cfg.CreateMap<KerbalSPGameSettingsApiModel, KerbalSPGameSettings>();
        }
    }
}