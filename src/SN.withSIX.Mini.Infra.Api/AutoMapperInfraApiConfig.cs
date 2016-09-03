// <copyright company="SIX Networks GmbH" file="AutoMapperInfraApiConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using AutoMapper;
using withSIX.Api.Models.Collections;
using SN.withSIX.Core;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.WebApi;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Api
{
    public class AutoMapperInfraApiConfig
    {
        public static void Setup(IProfileExpression cfg) {
            cfg.CreateMap<ModClientApiJson, ModNetworkContent>()
                .Include<ModClientApiJsonV3WithGameId, ModNetworkContent>()
                .BeforeMap((json, content) => {
                    content?.Publishers.Clear();
                })
                .ForMember(x => x.Version, opt => opt.MapFrom(src => src.LatestStableVersion ?? src.Version))
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.RecentInfo, opt => opt.Ignore());
            // Does not get inherited?!
            cfg.CreateMap<ModClientApiJsonV3WithGameId, ModNetworkContent>()
                .BeforeMap((json, content) => {
                    content?.Publishers.Clear();
                })
                .ForMember(x => x.Version, opt => opt.MapFrom(src => src.LatestStableVersion ?? src.Version))
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.RecentInfo, opt => opt.Ignore());

            cfg.CreateMap<ModClientApiJsonV3WithGameId, ModClientApiJsonV3WithGameId>();

            cfg.CreateMap<ContentPublisherApiJson, ContentPublisher>()
                .ConstructUsing(src => new ContentPublisher(src.Type, src.Id))
                .ForMember(x => x.Publisher, opt => opt.MapFrom(src => src.Type))
                .ForMember(x => x.PublisherId, opt => opt.MapFrom(src => src.Id));

            cfg.CreateMap<ContentDtoV2, NetworkContent>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                //.ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.RecentInfo, opt => opt.Ignore())
                .ForMember(x => x.Size, opt => opt.MapFrom(src => src.SizeWd))
                .ForMember(x => x.SizePacked, opt => opt.MapFrom(src => src.Size))
                .Include<ModDtoV2, ModNetworkContent>()
                .Include<MissionDtoV2, MissionNetworkContent>();

            // TODO: Why do the above includes not work??
            cfg.CreateMap<ModDtoV2, ModNetworkContent>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                //.ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.RecentInfo, opt => opt.Ignore())
                .ForMember(x => x.Version, opt => opt.MapFrom(src => src.GetVersion()));
            cfg.CreateMap<MissionDtoV2, MissionNetworkContent>()
                //.ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.RecentInfo, opt => opt.Ignore());

            cfg.CreateMap<CollectionVersionModel, SubscribedCollection>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.Id, opt => opt.Ignore());
            cfg.CreateMap<CollectionModelWithLatestVersion, SubscribedCollection>()
                .AfterMap((src, dst) => src.LatestVersion.MapTo(dst));

            cfg.CreateMap<CollectionServer, CollectionVersionServerModel>();
            cfg.CreateMap<CollectionVersionServerModel, CollectionServer>();

            cfg.CreateMap<ModDtoV2, ModDtoV2>();
        }
        /*
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
        */
    }
}