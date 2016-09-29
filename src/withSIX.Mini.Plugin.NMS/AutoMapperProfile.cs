// <copyright company="SIX Networks GmbH" file="AutoMapperProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using withSIX.Mini.Plugin.NMS.ApiModels;
using withSIX.Mini.Plugin.NMS.Models;

namespace withSIX.Mini.Plugin.NMS
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() {
            CreateMap<NMSGameSettings, NMSGameSettingsApiModel>();
            CreateMap<NMSGameSettingsApiModel, NMSGameSettings>();
        }
    }
}