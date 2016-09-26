// <copyright company="SIX Networks GmbH" file="SteamApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SteamLayerWrap;

namespace withSIX.Steam.Plugin.Arma
{
    public class SteamApi : ISteamApi
    {
        private readonly LockedWrapper<ISteamAPIWrap> _steamApi;

        public SteamApi(ISteamAPIWrap apiWrap, IScheduler scheduler) {
            _steamApi = new LockedWrapper<ISteamAPIWrap>(apiWrap, scheduler);
        }

        public Task<LockedWrapper<MatchmakingServiceWrap>> CreateMatchmakingServiceWrap()
            => _steamApi.Do(t => new LockedWrapper<MatchmakingServiceWrap>(t.CreateMatchmakingService(), _steamApi.Scheduler));

        public Task<LockedWrapper<ServerRulesServiceWrap>> CreateRulesManagerWrap()
            => _steamApi.Do(t => new LockedWrapper<ServerRulesServiceWrap>(t.CreateServerRulesService(), _steamApi.Scheduler));

        public async Task Initialize(IAbsoluteDirectoryPath gamePath, uint appId) {
            var r = await _steamApi.Do(x => {
                var managerConfigWrap = new ManagerConfigWrap {ConsumerAppId = appId};
                managerConfigWrap.Load(gamePath.GetChildFileWithName(@"Launcher\config.bin").ToString());
                return x.Init(managerConfigWrap);
            }).ConfigureAwait(false);
            if (r == InitResult.SteamNotRunning)
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            if (r == InitResult.APIInitFailed)
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            if (r == InitResult.ContextCreationFailed)
                throw new SteamInitializationException(
                    "Steam initialization failed. Is Steam running under the same priviledges?");
            if (r == InitResult.AlreadyInitialized)
                throw new SteamInitializationException(
                    "Steam initialization failed. Already initialized");
            if (r == InitResult.Disabled)
                throw new SteamInitializationException(
                    "Steam initialization failed. Disabled");
        }
    }

    public interface ISteamApi
    {
        Task Initialize(IAbsoluteDirectoryPath gamePath, uint appId);
        Task<LockedWrapper<MatchmakingServiceWrap>> CreateMatchmakingServiceWrap();
        Task<LockedWrapper<ServerRulesServiceWrap>> CreateRulesManagerWrap();
    }
}