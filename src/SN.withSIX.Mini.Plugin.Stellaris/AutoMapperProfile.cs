// <copyright company="SIX Networks GmbH" file="AutoMapperProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SN.withSIX.Mini.Plugin.Stellaris.ApiModels;
using SN.withSIX.Mini.Plugin.Stellaris.Models;

namespace SN.withSIX.Mini.Plugin.Stellaris
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() {
            CreateMap<StellarisGameSettings, StellarisGameSettingsApiModel>();
            CreateMap<StellarisGameSettingsApiModel, StellarisGameSettings>();
        }
    }
}