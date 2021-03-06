﻿// <copyright company="SIX Networks GmbH" file="AutoMapperAppConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoMapper;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Helpers;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using GameSettings = withSIX.Mini.Core.Games.GameSettings;
using SubscribedCollection = withSIX.Mini.Core.Games.SubscribedCollection;

namespace withSIX.Mini.Applications
{
    public class AutoMapperAppConfig : Profile
    {
        public AutoMapperAppConfig() {
            SetupSettingsTabs(this);
            SetupApi(this);

            CreateMap<string, IPEndPoint>()
                .ConstructUsing(src => src == null ? null : ParseEndpoint(src))
                .IgnoreAllMembers();
            CreateMap<string, IPAddress>()
                .ConstructUsing(src => src == null ? null : ParseIP(src))
                .IgnoreAllMembers();

            CreateMap<ProgressComponent, FlatProgressInfo>();
            CreateMap<ProgressLeaf, FlatProgressInfo>();
            CreateMap<ProgressContainer, FlatProgressInfo>();

            CreateMap<GameSettings, GameSettingsApiModel>()
                .ForMember(x => x.StartupLine, opt => opt.MapFrom(src => src.StartupParameters.StartupLine));
            CreateMap<GameSettingsApiModel, GameSettings>()
                .AfterMap((src, dest) => dest.StartupParameters.StartupLine = src.StartupLine);
        }

        private IPEndPoint ParseEndpoint(string str) {
            var split = str.Split(':');
            var ip = IPAddress.Parse(string.Join(":", split.Take(split.Length - 1)));
            var port = System.Convert.ToInt32(split.Last());
            return new IPEndPoint(ip, port);
        }

        private IPAddress ParseIP(string str) => IPAddress.Parse(str);

        static void SetupApi(IProfileExpression cfg) {
            cfg.CreateMap<Game, ClientContentInfo2>()
                .ForMember(x => x.RecentContent,
                    opt => opt.MapFrom(src => src.RecentItems.OrderByDescending(x => x.RecentInfo.LastUsed).Take(10)));

            cfg.CreateMap<LocalContent, InstalledContentModel>();

            cfg.CreateMap<LocalCollection, LocalCollectionModel>();

            cfg.CreateMap<Content, ContentModel>()
                .Include<LocalContent, ContentModel>()
                .Include<Collection, ContentModel>()
                .Include<LocalCollection, ContentModel>()
                .Include<NetworkCollection, ContentModel>()
                .Include<SubscribedCollection, ContentModel>()
                .Include<NetworkContent, ContentModel>()
                .Include<ModNetworkContent, ContentModel>()
                .Include<MissionNetworkContent, ContentModel>();

            cfg.CreateMap<LocalContent, ContentModel>();
            cfg.CreateMap<Collection, ContentModel>();
            cfg.CreateMap<LocalCollection, ContentModel>();
            cfg.CreateMap<NetworkCollection, ContentModel>();
            cfg.CreateMap<SubscribedCollection, ContentModel>();
            cfg.CreateMap<NetworkContent, ContentModel>();
            cfg.CreateMap<ModNetworkContent, ContentModel>();
            cfg.CreateMap<MissionNetworkContent, ContentModel>();

            cfg.CreateMap<NetworkContent, ContentModel>()
                .Include<ModNetworkContent, ContentModel>()
                .Include<MissionNetworkContent, ContentModel>();
            cfg.CreateMap<NetworkCollection, ContentModel>()
                .Include<SubscribedCollection, ContentModel>();

            cfg.CreateMap<SubscribedCollection, ContentModel>();
            cfg.CreateMap<ModNetworkContent, ContentModel>();
            cfg.CreateMap<MissionNetworkContent, ContentModel>();

            cfg.CreateMap<Content, RecentContentModel>()
                .Include<LocalContent, RecentContentModel>()
                .Include<Collection, RecentContentModel>()
                .Include<LocalCollection, RecentContentModel>()
                .Include<NetworkCollection, RecentContentModel>()
                .Include<SubscribedCollection, RecentContentModel>()
                .Include<NetworkContent, RecentContentModel>()
                .Include<ModNetworkContent, RecentContentModel>()
                .Include<MissionNetworkContent, RecentContentModel>();
            cfg.CreateMap<LocalContent, RecentContentModel>();
            cfg.CreateMap<Collection, RecentContentModel>();
            cfg.CreateMap<LocalCollection, RecentContentModel>();
            cfg.CreateMap<NetworkCollection, RecentContentModel>();
            cfg.CreateMap<SubscribedCollection, RecentContentModel>();
            cfg.CreateMap<NetworkContent, RecentContentModel>();
            cfg.CreateMap<ModNetworkContent, RecentContentModel>();
            cfg.CreateMap<MissionNetworkContent, RecentContentModel>();


            cfg.CreateMap<LocalContent, RecentContentModel>();
            cfg.CreateMap<NetworkContent, RecentContentModel>()
                .Include<ModNetworkContent, RecentContentModel>()
                .Include<MissionNetworkContent, RecentContentModel>();
            cfg.CreateMap<NetworkCollection, RecentContentModel>()
                .Include<SubscribedCollection, RecentContentModel>();

            cfg.CreateMap<ModNetworkContent, RecentContentModel>();
            cfg.CreateMap<MissionNetworkContent, RecentContentModel>();
            cfg.CreateMap<SubscribedCollection, RecentContentModel>();


            cfg.CreateMap<Content, FavoriteContentModel>()
                .Include<LocalContent, FavoriteContentModel>()
                .Include<Collection, FavoriteContentModel>()
                .Include<LocalCollection, FavoriteContentModel>()
                .Include<NetworkCollection, FavoriteContentModel>()
                .Include<SubscribedCollection, FavoriteContentModel>()
                .Include<NetworkContent, FavoriteContentModel>()
                .Include<ModNetworkContent, FavoriteContentModel>()
                .Include<MissionNetworkContent, FavoriteContentModel>();

            cfg.CreateMap<LocalContent, FavoriteContentModel>();
            cfg.CreateMap<Collection, FavoriteContentModel>();
            cfg.CreateMap<LocalCollection, FavoriteContentModel>();
            cfg.CreateMap<NetworkCollection, FavoriteContentModel>();
            cfg.CreateMap<SubscribedCollection, FavoriteContentModel>();
            cfg.CreateMap<NetworkContent, FavoriteContentModel>();
            cfg.CreateMap<ModNetworkContent, FavoriteContentModel>();
            cfg.CreateMap<MissionNetworkContent, FavoriteContentModel>();

            cfg.CreateMap<LocalContent, FavoriteContentModel>();
            cfg.CreateMap<NetworkContent, FavoriteContentModel>()
                .Include<ModNetworkContent, FavoriteContentModel>()
                .Include<MissionNetworkContent, FavoriteContentModel>();
            cfg.CreateMap<NetworkCollection, FavoriteContentModel>()
                .Include<SubscribedCollection, FavoriteContentModel>();

            cfg.CreateMap<ModNetworkContent, FavoriteContentModel>();
            cfg.CreateMap<MissionNetworkContent, FavoriteContentModel>();
            cfg.CreateMap<SubscribedCollection, FavoriteContentModel>();


            cfg.CreateMap<ContentStatusChanged, ContentState>()
                .Include<ContentStatusChanged, ContentStatus>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.Content.Id))
                .ForMember(x => x.GameId, opt => opt.MapFrom(src => src.Content.GameId))
                .ForMember(x => x.Size, opt => opt.MapFrom(src => src.Content.InstallInfo.Size))
                .ForMember(x => x.SizePacked, opt => opt.MapFrom(src => src.Content.InstallInfo.SizePacked))
                .ForMember(x => x.Version, opt => opt.MapFrom(src => src.Content.InstallInfo.Version))
                .ForMember(x => x.LastUsed, opt => opt.MapFrom(src => src.Content.RecentInfo.LastUsed))
                .ForMember(x => x.LastInstalled, opt => opt.MapFrom(src => src.Content.InstallInfo.LastInstalled))
                .ForMember(x => x.LastUpdated, opt => opt.MapFrom(src => src.Content.InstallInfo.LastUpdated));
            cfg.CreateMap<ContentStatusChanged, ContentStatus>();
            cfg.CreateMap<Content, ContentState>()
                .Include<Content, ContentStatus>()
                .ForMember(x => x.Size, opt => opt.MapFrom(src => src.InstallInfo.Size))
                .ForMember(x => x.SizePacked, opt => opt.MapFrom(src => src.InstallInfo.SizePacked))
                .ForMember(x => x.Version, opt => opt.MapFrom(src => src.InstallInfo.Version))
                .ForMember(x => x.LastUsed, opt => opt.MapFrom(src => src.RecentInfo.LastUsed))
                .ForMember(x => x.LastInstalled, opt => opt.MapFrom(src => src.InstallInfo.LastInstalled))
                .ForMember(x => x.LastUpdated, opt => opt.MapFrom(src => src.InstallInfo.LastUpdated));
            cfg.CreateMap<Content, ContentStatus>();
            // TODO: For collections we should look at the contents of the collection, and then determine if all the content is installed/uptodate
            // then determine the collection state based on that. for this we can't use automapper. We will have to manually do this by providing the games and checking their installed content.
            // Perhaps we should then cache this state too..
            //cfg.CreateMap<NetworkCollection, ContentState>();
            cfg.CreateMap<Game, GameApiModel>()
                // .Include<Game, GameHomeApiModel>()
                .ForMember(x => x.Name, opt => opt.MapFrom(src => GetName(src)))
                .ForMember(x => x.Slug, opt => opt.MapFrom(src => src.Metadata.Slug))
                .ForMember(x => x.Image, opt => opt.MapFrom(src => src.Metadata.Image))
                .ForMember(x => x.CollectionsCount, opt => opt.MapFrom(src => src.LocalCollections.Count()))
                // TODO .InstalledContent.OfType<ICollectionContent>()
                .ForMember(x => x.MissionsCount,
                    opt => opt.MapFrom(src => src.InstalledContent.OfType<IMissionContent>().Count()))
                .ForMember(x => x.ModsCount,
                    opt => opt.MapFrom(src => src.InstalledContent.OfType<IModContent>().Count()));
            //.ForMember(x => x.Author, opt => opt.MapFrom(src => src.Metadata.Author));

            cfg.CreateMap<Content, ContentApiModel>()
                .Include<NetworkContent, ContentApiModel>()
                .Include<LocalContent, ContentApiModel>()
                .ForMember(x => x.LastUsed, opt => opt.MapFrom(src => src.RecentInfo.LastUsed))
                .ForMember(x => x.LastInstalled, opt => opt.MapFrom(src => src.InstallInfo.LastInstalled))
                .ForMember(x => x.LastUpdated, opt => opt.MapFrom(src => src.InstallInfo.LastUpdated))
                .ForMember(x => x.Type, opt => opt.MapFrom(src => ConvertToType(src)));
            cfg.CreateMap<NetworkContent, ContentApiModel>()
                .Include<ModNetworkContent, ContentApiModel>()
                .Include<MissionNetworkContent, ContentApiModel>();
            cfg.CreateMap<LocalContent, ContentApiModel>()
                .Include<ModLocalContent, ContentApiModel>()
                .Include<MissionLocalContent, ContentApiModel>();
            cfg.CreateMap<ModLocalContent, ContentApiModel>()
                //.ForMember(x => x.InstalledVersion, opt => opt.MapFrom(src => src.InstallInfo.Version))
                .ForMember(x => x.Type, opt => opt.MapFrom(src => ConvertToType(src)));
            cfg.CreateMap<MissionLocalContent, ContentApiModel>()
                //.ForMember(x => x.InstalledVersion, opt => opt.MapFrom(src => src.InstallInfo.Version))
                .ForMember(x => x.Type, opt => opt.MapFrom(src => ConvertToType(src)));
            cfg.CreateMap<Collection, ContentApiModel>()
                .ForMember(x => x.Type, opt => opt.MapFrom(src => ConvertToType(src)))
                .Include<NetworkCollection, ContentApiModel>()
                .Include<LocalCollection, ContentApiModel>();
            cfg.CreateMap<NetworkCollection, ContentApiModel>();
            cfg.CreateMap<LocalCollection, ContentApiModel>();

            cfg.CreateMap<ModNetworkContent, ContentApiModel>();
            cfg.CreateMap<MissionNetworkContent, ContentApiModel>();
            cfg.CreateMap<ModLocalContent, ContentApiModel>();
            cfg.CreateMap<MissionLocalContent, ContentApiModel>();

            cfg.CreateMap<List<Game>, GamesApiModel>()
                .ForMember(x => x.Games, opt => opt.MapFrom(src => src.Select(x => x.MapTo<GameApiModel>()).ToList()));

            cfg.CreateMap<List<Game>, HomeApiModel>()
                .ForMember(x => x.Games, opt => opt.MapFrom(src => src.Select(x => x.MapTo<GameApiModel>()).ToList()))
                .ForMember(x => x.Recent,
                    opt => opt.MapFrom(src => src.SelectMany(x => x.RecentItems.Select(c => new {x, c}))
                        .OrderByDescending(x => x.c.RecentInfo.LastUsed)
                        .Take(10).Select(x => Convert(x.x, x.c)).OrderByDescending(x => x.LastUsed)))
                .ForMember(x => x.NewContent,
                    opt => opt.MapFrom(src => src.SelectMany(x => x.InstalledContent.Select(c => new {x, c}))
                        .OrderByDescending(x => x.c.InstallInfo.LastInstalled)
                        .Take(10).Select(x => Convert(x.x, x.c))
                        .OrderByDescending(x => x.LastInstalled)))
                .ForMember(x => x.Updates,
                    opt =>
                        opt.MapFrom(
                            src =>
                                src.SelectMany(
                                        x =>
                                            x.Updates.OrderByDescending(u => u.UpdatedVersion)
                                                .Take(24)
                                                .Select(c => new {x, c}))
                                    .OrderByDescending(x => x.c.UpdatedVersion)
                                    .Select(x => Convert(x.x, x.c))
                                    .Take(24)));

            cfg.CreateMap<Game, GameHomeApiModel>()
                .ForMember(x => x.Name, opt => opt.MapFrom(src => GetName(src)))
                .ForMember(x => x.Slug, opt => opt.MapFrom(src => src.Metadata.Slug))
                .ForMember(x => x.Image, opt => opt.MapFrom(src => src.Metadata.Image))
                .ForMember(x => x.InstalledModsCount,
                    opt => opt.MapFrom(src => src.InstalledContent.OfType<IModContent>().Count()))
                .ForMember(x => x.InstalledMissionsCount,
                    opt => opt.MapFrom(src => src.InstalledContent.OfType<IMissionContent>().Count()))
                .ForMember(x => x.Recent, opt => opt.MapFrom(src => src.RecentItems
                    .OrderByDescending(x => x.RecentInfo.LastUsed)
                    .Take(10).Select(x => Convert(src, x))
                    .OrderByDescending(x => x.LastUsed)))
                .ForMember(x => x.NewContent, opt => opt.MapFrom(src => src.AllAvailableContent
                    .OrderByDescending(x => x.InstallInfo.LastInstalled)
                    .Take(10).Select(x => Convert(src, x))
                    .OrderByDescending(x => x.LastInstalled)))
                .ForMember(x => x.Updates, opt => opt.MapFrom(src => src.Updates
                    .OrderByDescending(x => x.UpdatedVersion)
                    .Select(x => Convert(src, x))
                    .Take(24)));

            cfg.CreateMap<PageModel<ContentApiModel>, ModsApiModel>();
            cfg.CreateMap<PageModel<ContentApiModel>, MissionsApiModel>();
            cfg.CreateMap<PageModel<ContentApiModel>, CollectionsApiModel>();

            cfg.CreateMap<Game, MissionsApiModel>()
                .ConstructUsing((src, ctx) => src.AllAvailableContent
                    .OfType<IMissionContent>()
                    .Select(x => Convert(src, x))
                    .ToPageModelFromCtx(ctx).MapTo<MissionsApiModel>());
            cfg.CreateMap<Game, ModsApiModel>()
                .ConstructUsing((src, ctx) => {
                    var items = src.AllAvailableContent
                        .OfType<IModContent>()
                        .Select(x => Convert(src, x));
                    var pm = items.ToPageModelFromCtx(ctx);
                    return pm.MapTo<ModsApiModel>();
                });
            cfg.CreateMap<Game, CollectionsApiModel>()
                .ConstructUsing((src, ctx) => src.AllAvailableContent
                    .OfType<ICollectionContent>()
                    .Select(x => Convert(src, x))
                    .ToPageModelFromCtx(ctx).MapTo<CollectionsApiModel>());

            cfg.CreateMap<ActionNotification, ActionTabState>()
                .ForMember(x => x.Text, opt => opt.MapFrom(src => src.Title + " " + src.Text));

            cfg.CreateMap<ProgressInfo, ChildActionState>()
                .AfterMap((src, dest) => {
                    var ac = GetActiveComponent(src);
                    if (ac != null) {
                        dest.Progress = ac.Progress;
                        dest.Speed = ac.Speed;
                        dest.Title = ac.Title;
                    } else
                        dest.Title = "Processing";
                })
                .ForMember(x => x.Details, opt => opt.MapFrom(src => src.Text));
        }

        private static FlatProgressInfo GetActiveComponent(ProgressInfo info) {
            if (info.Components.Count >= 3)
                return info.Components[2];
            if (info.Components.Count == 2)
                return info.Components[1];
            return null;
        }

        private static string GetName(Game src) => src.Metadata.ShortName ?? src.Metadata.Name;

        // TODO: expand types.. missions etc
        // Or investigate reimplementation of ContentSlug, based on new ContentType that is available on every class, not just network content?
        static string ConvertToType(Content src) => src is ICollectionContent ? "collection" : "mod";

        static ContentApiModel Convert<T>(Game game, T item) where T : IContent {
            var convert = item.MapTo<ContentApiModel>();
            convert.GameId = game.Id;
            convert.GameSlug = game.Metadata.Slug;
            return convert;
        }

        static void SetupSettingsTabs(IProfileExpression cfg) {
            cfg.CreateMap<Settings, GeneralSettings>()
                .ForMember(x => x.ApiPort, opt => opt.MapFrom(src => src.Local.ApiPort))
                .ForMember(x => x.OptOutErrorReports, opt => opt.MapFrom(src => src.Local.OptOutReporting))
                .ForMember(x => x.EnableDesktopNotifications,
                    opt => opt.MapFrom(src => src.Local.ShowDesktopNotifications))
                .ForMember(x => x.UseSystemBrowser,
                    opt => opt.MapFrom(src => src.Local.UseSystemBrowser))
                .ForMember(x => x.EnableDiagnosticsMode,
                    opt => opt.MapFrom(src => src.Local.EnableDiagnosticsMode))
                .ForMember(x => x.LaunchWithWindows, opt => opt.MapFrom(src => src.Local.StartWithWindows))
                .ForMember(x => x.Version, opt => opt.UseValue(Consts.ProductVersion))
                .IgnoreAllOtherMembers();
            cfg.CreateMap<GeneralSettings, Settings>()
                .AfterMap((src, dest) => {
                    src.MapTo(dest.Local);
                    dest.Mapped();
                })
                .IgnoreAllMembers();
            cfg.CreateMap<GeneralSettings, LocalSettings>()
                .ForMember(x => x.OptOutReporting, opt => opt.MapFrom(src => src.OptOutErrorReports))
                .ForMember(x => x.ShowDesktopNotifications,
                    opt => opt.MapFrom(src => src.EnableDesktopNotifications))
                .ForMember(x => x.UseSystemBrowser,
                    opt => opt.MapFrom(src => src.UseSystemBrowser))
                .ForMember(x => x.EnableDiagnosticsMode,
                    opt => opt.MapFrom(src => src.EnableDiagnosticsMode))
                .ForMember(x => x.StartWithWindows, opt => opt.MapFrom(src => src.LaunchWithWindows))
                .IgnoreAllOtherMembers();
        }
    }
}