// <copyright company="SIX Networks GmbH" file="AutoMapperAppConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Applications
{
    public class AutoMapperAppConfig
    {
        public static void Setup(IProfileExpression cfg) {
            SetupSettingsTabs(cfg);
            SetupApi(cfg);

            cfg.CreateMap<ProgressComponent, FlatProgressInfo>();
            cfg.CreateMap<ProgressLeaf, FlatProgressInfo>();
            cfg.CreateMap<ProgressContainer, FlatProgressInfo>();
        }

        static void SetupApi(IProfileExpression cfg) {
            cfg.CreateMap<Game, ClientContentInfo2>()
                .ForMember(x => x.FavoriteContent, opt => opt.MapFrom(src => src.FavoriteItems))
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
                .ForMember(x => x.TypeScope, opt => opt.ResolveUsing(GetTypeScope))
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
                .ForMember(x => x.Favorites,
                    opt => opt.MapFrom(src => src.SelectMany(x => x.FavoriteItems.Select(c => new {x, c}))
                        .Take(10).Select(x => Convert(x.x, x.c))))
                .ForMember(x => x.NewContent,
                    opt => opt.MapFrom(src => src.SelectMany(x => x.InstalledContent.Select(c => new {x, c}))
                        .OrderByDescending(x => x.c.InstallInfo.LastInstalled)
                        .Take(10).Select(x => Convert(x.x, x.c))
                        .OrderByDescending(x => x.LastInstalled)))
                .ForMember(x => x.Updates,
                    opt => opt.MapFrom(src => src.SelectMany(x => x.Updates.Select(c => new {x, c}))
                        .Select(x => Convert(x.x, x.c))
                        .OrderByDescending(x => x.UpdatedVersion)));

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
                .ForMember(x => x.Favorites, opt => opt.MapFrom(src => src.FavoriteItems
                    .Select(x => Convert(src, x))))
                .ForMember(x => x.NewContent, opt => opt.MapFrom(src => src.InstalledContent
                    .OrderByDescending(x => x.InstallInfo.LastInstalled)
                    .Take(10).Select(x => Convert(src, x))
                    .OrderByDescending(x => x.LastInstalled)))
                .ForMember(x => x.Updates, opt => opt.MapFrom(src => src.Updates
                    .Select(x => Convert(src, x))
                    .OrderByDescending(x => x.UpdatedVersion)));

            cfg.CreateMap<Game, GameMissionsApiModel>()
                .ForMember(x => x.Missions,
                    opt =>
                        opt.MapFrom(src => src.InstalledContent.OfType<IMissionContent>().Select(x => Convert(src, x))));
            cfg.CreateMap<Game, GameModsApiModel>()
                .ForMember(x => x.Mods,
                    opt => opt.MapFrom(src => src.InstalledContent.OfType<IModContent>().Select(x => Convert(src, x))));
            cfg.CreateMap<Game, GameCollectionsApiModel>()
                .ForMember(x => x.Collections,
                    opt =>
                        opt.MapFrom(
                            src => src.InstalledContent.OfType<ICollectionContent>().Select(x => Convert(src, x))));

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

        private static object GetTypeScope(Collection x) {
            if (x is SubscribedCollection)
                return TypeScope.Subscribed;
            if (x is LocalCollection)
                return TypeScope.Local;
            return TypeScope.Published;
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
                .IgnoreAllMembers()
                .ForMember(x => x.ApiPort, opt => opt.MapFrom(src => src.Local.ApiPort))
                .ForMember(x => x.OptOutErrorReports, opt => opt.MapFrom(src => src.Local.OptOutReporting))
                .ForMember(x => x.EnableDesktopNotifications,
                    opt => opt.MapFrom(src => src.Local.ShowDesktopNotifications))
                .ForMember(x => x.UseSystemBrowser,
                    opt => opt.MapFrom(src => src.Local.UseSystemBrowser))
                .ForMember(x => x.EnableDiagnosticsMode,
                    opt => opt.MapFrom(src => src.Local.EnableDiagnosticsMode))
                .ForMember(x => x.LaunchWithWindows, opt => opt.MapFrom(src => src.Local.StartWithWindows))
                .ForMember(x => x.Version, opt => opt.UseValue(Consts.ProductVersion));
            cfg.CreateMap<GeneralSettings, Settings>()
                .IgnoreAllMembers()
                .AfterMap((src, dest) => src.MapTo(dest.Local));
            cfg.CreateMap<GeneralSettings, LocalSettings>()
                .IgnoreAllMembers()
                .ForMember(x => x.OptOutReporting, opt => opt.MapFrom(src => src.OptOutErrorReports))
                .ForMember(x => x.ShowDesktopNotifications,
                    opt => opt.MapFrom(src => src.EnableDesktopNotifications))
                .ForMember(x => x.UseSystemBrowser,
                    opt => opt.MapFrom(src => src.UseSystemBrowser))
                .ForMember(x => x.EnableDiagnosticsMode,
                    opt => opt.MapFrom(src => src.EnableDiagnosticsMode))
                .ForMember(x => x.StartWithWindows, opt => opt.MapFrom(src => src.LaunchWithWindows));
        }
    }
}