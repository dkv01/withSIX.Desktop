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
                .ForMember(x => x.ConnectionEndPoint,
                    opt => opt.MapFrom(src => new IPEndPoint(src.Address.Address, src.Port)))
                .ForMember(x => x.Mission, opt => opt.MapFrom(src => src.Game))
                .ForMember(x => x.Tags, opt => opt.MapFrom(src => src.Keywords))
                .ForMember(x => x.RequirePassword, opt => opt.MapFrom(src => src.Visibility == 1))
                .ForMember(x => x.IsVacEnabled, opt => opt.MapFrom(src => src.Vac == 1))
                .ForMember(x => x.GameTags,
                    opt => opt.MapFrom(src => src.Keywords == null ? null : GameTags.Parse(src.Keywords)));
        }
    }
}