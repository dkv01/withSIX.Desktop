// <copyright company="SIX Networks GmbH" file="AutoMapperInfraApiConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using AutoMapper;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Infra.Data.ApiModels;
using CollectionServer = withSIX.Mini.Core.Games.CollectionServer;
using SubscribedCollection = withSIX.Mini.Core.Games.SubscribedCollection;

namespace withSIX.Mini.Infra.Data
{
    public class AutoMapperInfraDataConfig : Profile
    {
        public AutoMapperInfraDataConfig() {
            CreateMap<ModClientApiJson, ModNetworkContent>()
                .Include<ModClientApiJsonV3WithGameId, ModNetworkContent>()
                .BeforeMap((json, content) => { content?.Publishers.Clear(); })
                .ForMember(x => x.Version, opt => opt.ResolveUsing(src => src.GetVersion()))
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.RecentInfo, opt => opt.Ignore());
            // Does not get inherited?!
            CreateMap<ModClientApiJsonV3WithGameId, ModNetworkContent>()
                .BeforeMap((json, content) => { content?.Publishers.Clear(); })
                .ForMember(x => x.Version, opt => opt.ResolveUsing(src => src.GetVersion()))
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.RecentInfo, opt => opt.Ignore());

            CreateMap<ModClientApiJsonV3WithGameId, ModClientApiJsonV3WithGameId>();

            CreateMap<ContentPublisherApiJson, ContentPublisher>()
                .ConstructUsing(src => new ContentPublisher(src.Type, src.Id))
                .ForMember(x => x.Publisher, opt => opt.MapFrom(src => src.Type))
                .ForMember(x => x.PublisherId, opt => opt.MapFrom(src => src.Id));

            CreateMap<ContentDtoV2, NetworkContent>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                //.ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.RecentInfo, opt => opt.Ignore())
                .ForMember(x => x.Size, opt => opt.MapFrom(src => src.SizeWd))
                .ForMember(x => x.SizePacked, opt => opt.MapFrom(src => src.Size))
                .Include<ModDtoV2, ModNetworkContent>()
                .Include<MissionDtoV2, MissionNetworkContent>();

            // TODO: Why do the above includes not work??
            CreateMap<ModDtoV2, ModNetworkContent>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                //.ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.RecentInfo, opt => opt.Ignore())
                .ForMember(x => x.Version, opt => opt.ResolveUsing(src => src.GetVersion()));
            CreateMap<MissionDtoV2, MissionNetworkContent>()
                //.ForMember(x => x.Aliases, opt => opt.ResolveUsing(ResolveAliases))
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.RecentInfo, opt => opt.Ignore());

            CreateMap<CollectionVersionModel, SubscribedCollection>()
                .ForMember(x => x.Dependencies, opt => opt.Ignore())
                .ForMember(x => x.Id, opt => opt.Ignore());
            CreateMap<CollectionModelWithLatestVersion, SubscribedCollection>()
                .AfterMap((src, dst) => src.LatestVersion.MapTo(dst))
                .ForMember(x => x.IsOwner,
                    opt => opt.ResolveUsing((src, dst, b, ctx) => src.AuthorId == (Guid) ctx.Items["user-id"]));

            CreateMap<CollectionServer, CollectionVersionServerModel>()
                .ForMember(x => x.Address, opt => opt.MapFrom(src => src.Address.ToString()));
            CreateMap<CollectionVersionServerModel, CollectionServer>()
                .ForMember(x => x.Address, opt => opt.MapFrom(src => IPEndPointConverter.ToIpEndpoint(src.Address)));

            CreateMap<ModDtoV2, ModDtoV2>();
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