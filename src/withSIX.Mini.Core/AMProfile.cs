// <copyright company="SIX Networks GmbH" file="AMProfile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Net;
using AutoMapper;
using GameServerQuery.Games.RV;
using GameServerQuery.Parsers;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Servers;
using withSIX.Api.Models.Servers.RV;

namespace withSIX.Mini.Core
{
    public class AMProfile : Profile
    {
        public AMProfile() {
            CreateMap<SourceParseResult, Server>()
                .SourceParseResultToServer();
            CreateMap<SourceParseResult, ArmaServer>()
                .SourceParseResultToServer()
                .AfterMap((src, dest) => {
                    var tags = src.Keywords;
                    if (tags != null) {
                        var p = GameTags.Parse(tags);
                        p.MapTo(dest);
                    }
                });
        }
    }

    internal static class Exts
    {
        internal static IMappingExpression<T1, T2> SourceParseResultToServer<T1, T2>(this IMappingExpression<T1, T2> cfg)
            where T1 : SourceParseResult where T2 : Server
        => cfg.ForMember(x => x.Game, opt => opt.MapFrom(src => TryUrlDecode(src.Game)))
            .ForMember(x => x.IsPasswordProtected, opt => opt.MapFrom(src => src.Visibility > 0))
            .ForMember(x => x.IsDedicated, opt => opt.MapFrom(src => src.ServerType > 0))
            .ForMember(x => x.QueryAddress, opt => opt.MapFrom(src => src.Address))
            .ForMember(x => x.ConnectionAddress, opt =>
                opt.MapFrom(
                    src => new IPEndPoint(src.Address.Address, src.Port > 0 ? src.Port : src.Address.Port - 1)));

        static string TryUrlDecode(string str) {
            if (str.Contains("%")) {
                try {
                    return WebUtility.UrlDecode(str);
                } catch (Exception) {
                }
            }
            return str;
        }
    }
}