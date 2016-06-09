// <copyright company="SIX Networks GmbH" file="AutoMapperPluginKerbalConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SN.withSIX.Mini.Plugin.Kerbal.ApiModels;
using SN.withSIX.Mini.Plugin.Kerbal.Models;

namespace SN.withSIX.Mini.Plugin.Kerbal
{
    public class AutoMapperPluginKerbalConfig
    {
        public static void Setup(IMapperConfiguration cfg) {
            SetupApiModels(cfg);
        }

        static void SetupApiModels(IMapperConfiguration cfg) {
            cfg.CreateMap<KerbalSPGameSettings, KerbalSPGameSettingsApiModel>();
            cfg.CreateMap<KerbalSPGameSettingsApiModel, KerbalSPGameSettings>();
        }
    }
}