// <copyright company="SIX Networks GmbH" file="AMProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using AutoMapper;
using GameServerQuery.Parsers;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Core
{
    public class AMProfile : Profile
    {
        public AMProfile() {
            CreateMap<SourceParseResult, ServerInfo>()
                .Include<SourceParseResult, ArmaServerInfo>();
            CreateMap<SourceParseResult, ArmaServerInfo>();
        }
    }
}