// <copyright company="SIX Networks GmbH" file="UserSettingsCacheManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using withSIX.Core.Infra.Cache;
using withSIX.Core.Infra.Services;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Infra.Data.Services
{
    public interface IUserSettingsCacheManager
    {
        IObservable<UserSettings> Get(Version version);
        IObservable<SettingsIndex> GetIndex();
        Task Set(UserSettings settings);
        Task Save();
    }

    public class UserSettingsCacheManager : IInfrastructureService, IUserSettingsCacheManager
    {
        const string IndexKey = SettingsVersion.SettingsPrefix + "_index";
        readonly IUserCache _cache;

        public UserSettingsCacheManager(IUserCache cache) {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            _cache = cache;
        }

        public IObservable<UserSettings> Get(Version version) {
            if (version == null) throw new ArgumentNullException(nameof(version));

            return _cache.GetOrFetchObject(GetKey(version), () => TryToFindOlderVersion(version));
        }

        public IObservable<SettingsIndex> GetIndex() => _cache.GetObject<SettingsIndex>(IndexKey);

        public async Task Save() {
            await _cache.Flush();
        }

        public async Task Set(UserSettings settings) {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var versions = await GetOrCreateVersionsIndex();
            versions.Register(settings.Version);
            await _cache.InsertObject(GetKey(settings.Version), settings);
            await SetVersions(versions);
        }

        static string GetKey(Version version) => new SettingsVersion(version).SettingsKey();

        async Task<UserSettings> TryToFindOlderVersion(Version version) {
            var userSettings = await FindOlderVersionOrCreateNew().ConfigureAwait(false);
            userSettings.Version = version;
            return userSettings;
        }

        async Task SetVersions(SettingsIndex versions) {
            await _cache.InsertObject(IndexKey, versions);
        }

        async Task<UserSettings> FindOlderVersionOrCreateNew() {
            var index = await GetOrCreateVersionsIndex();
            foreach (var v in index.GetOrdered()) {
                try {
                    return await _cache.GetObject<UserSettings>(v.SettingsKey());
                } catch (KeyNotFoundException) {}
            }

            return new UserSettings();
        }

        IObservable<SettingsIndex> GetOrCreateVersionsIndex() => _cache.GetOrCreateObject(IndexKey, () => new SettingsIndex());
    }

    public class SettingsIndex
    {
        public SettingsIndex() {
            Versions = new List<SettingsVersion>();
        }

        public List<SettingsVersion> Versions { get; set; }

        public IOrderedEnumerable<SettingsVersion> GetOrdered() => Versions.OrderByDescending(x => x.ToVersion());

        public void Register(Version version) {
            if (!HasVersion(version))
                Versions.Add(new SettingsVersion(version));
        }

        bool HasVersion(Version version) => Versions.Any(x => x.Major == version.Major && x.Minor == version.Minor);
    }

    public class SettingsVersion
    {
        public const string SettingsPrefix = "settings";
        public SettingsVersion() {}

        public SettingsVersion(Version version) : this(version.Major, version.Minor) {
            if (version == null) throw new ArgumentNullException(nameof(version));
        }

        public SettingsVersion(int major, int minor) {
            if (!(major >= 0)) throw new ArgumentNullException("major >= 0");
            if (!(minor >= 0)) throw new ArgumentNullException("minor >= 0");

            Major = major;
            Minor = minor;
        }

        public int Major { get; set; }
        public int Minor { get; set; }

        public string SettingsKey() => SettingsPrefix + "_" + Major + "." + Minor;

        public Version ToVersion() => new Version(Major, Minor);
    }
}