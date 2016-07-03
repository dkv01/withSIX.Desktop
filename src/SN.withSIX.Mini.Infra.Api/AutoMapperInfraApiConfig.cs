// <copyright company="SIX Networks GmbH" file="AutoMapperInfraApiConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using AutoMapper;
using SN.withSIX.Api.Models.Collections;
using SN.withSIX.Core;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.WebApi;
using CollectionModelWithLatestVersion = SN.withSIX.Mini.Infra.Api.WebApi.CollectionModelWithLatestVersion;

namespace SN.withSIX.Mini.Infra.Api
{
    public class AutoMapperInfraApiConfig
    {
        public static void Setup(IProfileExpression cfg) {
            cfg.CreateMap<ContentDto, NetworkContent>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.Image,
                    opt =>
                        opt.MapFrom(
                            src =>
                                src.ImagePath == null
                                    ? null
                                    : new Uri(CommonUrls.UsercontentCdnProduction, src.ImagePath)))
                .ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.RecentInfo, opt => opt.Ignore())
                .ForMember(x => x.IsFavorite, opt => opt.Ignore())
                .ForMember(x => x.Size, opt => opt.MapFrom(src => src.SizeWd))
                .ForMember(x => x.SizePacked, opt => opt.MapFrom(src => src.Size))
                .Include<ModDto, ModNetworkContent>()
                .Include<MissionDto, MissionNetworkContent>();

            // TODO: Why do the above includes not work??
            cfg.CreateMap<ModDto, ModNetworkContent>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.RecentInfo, opt => opt.Ignore())
                .ForMember(x => x.IsFavorite, opt => opt.Ignore())
                .ForMember(x => x.Version, opt => opt.MapFrom(src => src.GetVersion()));
            cfg.CreateMap<MissionDto, MissionNetworkContent>()
                .ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.RecentInfo, opt => opt.Ignore())
                .ForMember(x => x.IsFavorite, opt => opt.Ignore());

            cfg.CreateMap<CollectionVersionModel, SubscribedCollection>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.Id, opt => opt.Ignore());
            cfg.CreateMap<CollectionModelWithLatestVersion, SubscribedCollection>()
                .ForMember(x => x.Image,
                    opt => opt.MapFrom(src => src.AvatarUrl == null ? null : "https:" + src.AvatarUrl))
                .AfterMap((src, dst) => src.LatestVersion.MapTo(dst));

            cfg.CreateMap<CollectionServer, CollectionVersionServerModel>();
            cfg.CreateMap<CollectionVersionServerModel, CollectionServer>();

            cfg.CreateMap<ModDto, ModDto>();
        }

        static IEnumerable<string> ResolveAliases(ModDto arg) {
            if (arg.Aliases != null) {
                foreach (var e in arg.Aliases.Split(';'))
                    yield return e;
            }
            if (arg.CppName != null)
                yield return arg.CppName;
        }

        static IEnumerable<string> ResolveAliases(ContentDto arg) {
            if (arg.Aliases == null)
                yield break;
            foreach (var e in arg.Aliases.Split(';'))
                yield return e;
        }
    }
}