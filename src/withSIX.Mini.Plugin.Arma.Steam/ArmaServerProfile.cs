// <copyright company="SIX Networks GmbH" file="ArmaServerProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using SteamLayerWrap;
using withSIX.Mini.Plugin.Arma.Services;

namespace withSIX.Steam.Plugin.Arma
{
    public class ArmaServerProfile : Profile
    {
        public ArmaServerProfile() {
            CreateMap<ArmaServerInfo, ArmaServerInfoModel>();
            CreateMap<ServerModInfo, GameServerQuery.Games.RV.ServerModInfo>();
        }
    }
}