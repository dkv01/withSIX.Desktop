// <copyright company="SIX Networks GmbH" file="ArmaServerProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Net;
using AutoMapper;
using GameServerQuery.Games.RV;
using GameServerQuery.Parsers;
using withSIX.Steam.Core.Services;
using ServerModInfo = SteamLayerWrap.ServerModInfo;

namespace withSIX.Steam.Plugin.Arma
{
    public class ArmaServerProfile : Profile
    {
        public ArmaServerProfile() {
            CreateMap<ArmaServerInfo, ArmaServerInfoModel>();
            CreateMap<ServerModInfo, GameServerQuery.Games.RV.ServerModInfo>();
            CreateMap<SourceParseResult, ArmaServerInfoModel>()
                .ConstructUsing((src) => new ArmaServerInfoModel(src.Address))
            .ForMember(x => x.ConnectionEndPoint, opt =>
                opt.MapFrom(
                    src => new IPEndPoint(src.Address.Address, src.Port > 0 ? src.Port : src.Address.Port - 1)))
                .ForMember(x => x.Mission, opt => opt.MapFrom(src => src.Game))
                //.ForMember(x => x.IsDedicated, opt => opt.MapFrom(src => src.ServerType > 0))
                .ForMember(x => x.Tags, opt => opt.MapFrom(src => src.Keywords))
                .ForMember(x => x.RequirePassword, opt => opt.MapFrom(src => src.Visibility > 0))
                .ForMember(x => x.IsVacEnabled, opt => opt.MapFrom(src => src.Vac > 0))
                .ForMember(x => x.GameTags,
                    opt => opt.MapFrom(src => src.Keywords == null ? null : GameTags.Parse(src.Keywords)));
        }
    }
}