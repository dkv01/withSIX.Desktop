// <copyright company="SIX Networks GmbH" file="AMProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using GameServerQuery.Parsers;
using withSIX.Api.Models.Servers;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Core
{
    public class AMProfile : Profile
    {
        public AMProfile() {
            CreateMap<SourceParseResult, Server>()
                .Include<SourceParseResult, ArmaServer>()
                .ForMember(x => x.Mission, opt => opt.MapFrom(src => src.Game + "." + src.Map))
                .ForMember(x => x.IsPasswordProtected, opt => opt.MapFrom(src => src.Visibility > 0))
                .ForMember(x => x.IsDedicated, opt => opt.MapFrom(src => src.ServerType > 0));
            CreateMap<SourceParseResult, ArmaServer>();
        }
    }
}