// <copyright company="SIX Networks GmbH" file="AutoMapperPluginArmaConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Net;
using AutoMapper;
using GameServerQuery.Games.RV;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Servers;
using withSIX.Api.Models.Servers.RV;
using withSIX.Api.Models.Servers.RV.Arma3;
using withSIX.Mini.Applications.Factories;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Plugin.Arma.ApiModels;
using withSIX.Mini.Plugin.Arma.Models;
using withSIX.Mini.Plugin.Arma.Services;
using withSIX.Steam.Core.Services;
using ServerModInfo = GameServerQuery.Games.RV.ServerModInfo;

namespace withSIX.Mini.Plugin.Arma
{
    public class AutoMapperPluginArmaConfig : Profile
    {
        public AutoMapperPluginArmaConfig() {
            SetupApiModels();

            CreateMap<ArmaServerInfoModel, ArmaServer>()
                .ArmaServerInfoModelToArmaServer();

            CreateMap<ArmaServerInfoModel, ArmaServerInclRules>()
                .ArmaServerInfoModelToArmaServer();

            CreateMap<GameTags, Server>();
            CreateMap<GameTags, ArmaServer>()
                .GameTagsToArmaServer();
            CreateMap<GameTags, ArmaServerInclRules>()
                .ForMember(x => x.Difficulty, opt => opt.Ignore())
               .GameTagsToArmaServer();

            CreateMap<ServerModInfo, Api.Models.Servers.ServerModInfo>();
        }

        void SetupApiModels() {
            CreateMap<Arma2COGameSettings, Arma2COGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<Arma2COGameSettingsApiModel, Arma2COGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
            CreateMap<Arma3GameSettings, Arma3GameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<Arma3GameSettingsApiModel, Arma3GameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
            CreateMap<DayZGameSettings, DayZGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<DayZGameSettingsApiModel, DayZGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
            CreateMap<IronFrontGameSettings, IronFrontGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<IronFrontGameSettingsApiModel, IronFrontGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
            CreateMap<TakeOnHelicoptersGameSettings, TakeOnHelicoptersGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<TakeOnHelicoptersGameSettingsApiModel, TakeOnHelicoptersGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
            CreateMap<TakeOnMarsGameSettings, TakeOnMarsGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<TakeOnMarsGameSettingsApiModel, TakeOnMarsGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
            CreateMap<CarrierCommandGameSettings, CarrierCommandGameSettingsApiModel>()
                .IncludeBase<GameSettings, GameSettingsApiModel>();
            CreateMap<CarrierCommandGameSettingsApiModel, CarrierCommandGameSettings>()
                .IncludeBase<GameSettingsApiModel, GameSettings>();
        }
    }

    internal static class Exts
    {
        internal static IMappingExpression<T1, T2> ArmaServerInfoModelToArmaServer<T1, T2>(
            this IMappingExpression<T1, T2> cfg)
            where T1 : ArmaServerInfoModel where T2 : ArmaServer =>
            cfg.AfterMap((src, dest) => src.GameTags?.MapTo(dest))
                //.ForMember(x => x.Location, opt => opt.ResolveUsing<LocationResolver2>())
                .ForMember(x => x.ConnectionAddress, opt => opt.MapFrom(src => src.ConnectionEndPoint))
                .ForMember(x => x.QueryAddress, opt => opt.MapFrom(src => src.QueryEndPoint))
                .ForMember(x => x.Game, opt => opt.MapFrom(src => TryUrlDecode(src.Mission)))
                .ForMember(x => x.IsPasswordProtected, opt => opt.MapFrom(src => src.RequirePassword))
                .ForMember(x => x.Version, opt => opt.ResolveUsing(src => {
                    if (src.Version == null)
                        return null;
                    Version v;
                    return Version.TryParse(src.Version, out v) ? v : null;
                }));

        internal static IMappingExpression<T1, T2> GameTagsToArmaServer<T1, T2>(this IMappingExpression<T1, T2> cfg)
            where T1 : GameTags where T2 : ArmaServer
        => cfg.ForMember(x => x.Country, opt => opt.Ignore())
            .ForMember(x => x.BattlEye, opt => opt.MapFrom(src => src.BattlEye.GetValueOrDefault()))
            .ForMember(x => x.VerifySignatures, opt => opt.MapFrom(src => src.VerifySignatures.GetValueOrDefault()))
            .ForMember(x => x.Platform,
                opt => opt.MapFrom(src => src.Platform == 'w' ? ServerPlatform.Windows : ServerPlatform.Linux))
            .ForMember(x => x.IsLocked, opt => opt.MapFrom(src => src.Lock))
            .ForMember(x => x.ServerVersion, opt => opt.MapFrom(src => src.Version))
            .ForMember(x => x.Version, opt => opt.Ignore());

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