// <copyright company="SIX Networks GmbH" file="AutoMapperProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SN.withSIX.Mini.Plugin.CE.ApiModels;
using SN.withSIX.Mini.Plugin.CE.Models;

namespace SN.withSIX.Mini.Plugin.CE
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() {
            CreateMap<SkyrimGameSettings, SkyrimGameSettingsApiModel>();
            CreateMap<SkyrimGameSettingsApiModel, SkyrimGameSettings>();
            CreateMap<Fallout4GameSettings, Fallout4GameSettingsApiModel>();
            CreateMap<Fallout4GameSettingsApiModel, Fallout4GameSettings>();
        }
    }
}