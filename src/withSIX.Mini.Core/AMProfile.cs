// <copyright company="SIX Networks GmbH" file="AMProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Net;
using AutoMapper;
using GameServerQuery.Parsers;
using withSIX.Api.Models.Servers;

namespace withSIX.Mini.Core
{
    public class AMProfile : Profile
    {
        public AMProfile() {
            CreateMap<SourceParseResult, Server>()
                .ForMember(x => x.QueryAddress, opt => opt.MapFrom(src => src.Address))
                .ForMember(x => x.Mission, opt => opt.MapFrom(src => src.Game + "." + src.Map))
                .ForMember(x => x.IsPasswordProtected, opt => opt.MapFrom(src => src.Visibility > 0))
                .ForMember(x => x.IsDedicated, opt => opt.MapFrom(src => src.ServerType > 0));
            CreateMap<SourceParseResult, ArmaServer>()
                .IncludeBase<SourceParseResult, Server>()
                .ForMember(x => x.ConnectionAddress,
                    opt => opt.MapFrom(src => new IPEndPoint(src.Address.Address, src.Port)));
        }
    }
}