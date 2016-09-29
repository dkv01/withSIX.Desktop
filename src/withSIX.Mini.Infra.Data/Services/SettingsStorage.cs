// <copyright company="SIX Networks GmbH" file="SettingsStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using withSIX.Core.Infra.Cache;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Infra.Data.Services
{
    // Singleton for now..
    public class SettingsStorage : ContextBase<Settings>, IInfrastructureService, ISettingsStorage
    {
        readonly ILocalCache _localCache;
        readonly IUserCache _roamingCache;
        readonly ISecureCache _roamingSecureCache;

        public SettingsStorage(ILocalCache localCache, ISecureCache roamingSecureCache, IUserCache roamingCache) {
            _localCache = localCache;
            _roamingSecureCache = roamingSecureCache;
            _roamingCache = roamingCache;
        }

        public Task<Settings> GetSettings() => Get();

        protected override async Task SaveChangesInternal(Settings loaded) {
            AddTransactionCallback(() => new SettingsUpdated(loaded).Raise());
            await _localCache.InsertObject("localSettings", loaded.Local);
            await _roamingCache.InsertObject("roamingSettings", loaded.Roaming);
            await _roamingSecureCache.InsertObject("secureSettings", loaded.Secure);
        }

        protected override Task<Settings> LoadInternal() => Task.Run(LoadSettingsInternal);

        async Task<Settings> LoadSettingsInternal() {
            using (this.Bench()) {
                return new Settings(
                    await _roamingCache.GetOrCreateObject("roamingSettings", () => new RoamingSettings()),
                    await _roamingSecureCache.GetOrCreateObject("secureSettings", () => new SecureSettings()),
                    await _localCache.GetOrCreateObject("localSettings", () => new LocalSettings()));
            }
        }
    }
}