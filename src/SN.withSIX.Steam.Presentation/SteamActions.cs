// <copyright company="SIX Networks GmbH" file="SteamActions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Steam.Api;
using SN.withSIX.Steam.Api.Services;
using SN.withSIX.Steam.Core;
using SteamLayerWrap;
using Steamworks;
using withSIX.Steam.Plugin.Arma;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;
using SteamApi = withSIX.Steam.Plugin.Arma.SteamApi;

namespace SN.withSIX.Steam.Presentation
{
    public class SteamActions
    {
        public static Task PerformArmaSteamAction(Func<ISteamApi, Task> action, uint appId,
                ISteamSessionFactory steamSessionFactory)
            => PerformArmaSteamAction(x => action(x).Void(), appId, steamSessionFactory);

        public static async Task<T> PerformArmaSteamAction<T>(Func<ISteamApi, Task<T>> action, uint appId,
            ISteamSessionFactory steamSessionFactory) {
            var steamHelper = SteamHelper.Create();
            using (var wrap = new SteamAPIWrap()) {
                SteamApi steamApi = null;
                return await steamSessionFactory
                    .Do(appId, steamHelper.SteamPath,
                        async s => {
                            steamApi = new SteamApi(wrap, s);
                            await
                                steamApi.Initialize(steamHelper.TryGetSteamAppById(appId).AppPath, appId)
                                    .ConfigureAwait(false);
                        }, SteamAPI.RunCallbacks /* wrap.Simulate */, () => action(steamApi)).ConfigureAwait(false);
            }
        }

        public static async Task<ServerBrowser> CreateServerBrowser(ISteamApi steamApi)
            => new ServerBrowser(await steamApi.CreateMatchmakingServiceWrap().ConfigureAwait(false),
                async ep =>
                    new ServerInfoRulesFetcher(ep, await steamApi.CreateRulesManagerWrap().ConfigureAwait(false),
                        await steamApi.CreateMatchmakingServiceWrap().ConfigureAwait(false)));
    }
}