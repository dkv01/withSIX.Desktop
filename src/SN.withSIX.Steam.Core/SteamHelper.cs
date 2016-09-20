// <copyright company="SIX Networks GmbH" file="SteamHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Steam.Core.SteamKit.Utils;
using SteamKit2;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Steam.Core
{
    public class SteamStuff
    {
        private readonly IAbsoluteDirectoryPath _steamPath;

        public SteamStuff(IAbsoluteDirectoryPath steamPath) {
            _steamPath = steamPath;
        }

        public KeyValue TryReadSteamConfig() {
            try {
                return ReadSteamConfig();
            } catch (ParseException ex) {
                //this.Logger().FormattedFormattedWarnException(ex, "during steam config parsing");
                return null;
            }
        }

        KeyValue ReadSteamConfig() {
            if (_steamPath == null || !_steamPath.Exists)
                return null;

            var steamConfigPath = _steamPath.GetChildDirectoryWithName("config").GetChildFileWithName("config.vdf");
            return steamConfigPath.Exists
                ? KeyValueHelper.LoadFromFile(steamConfigPath)
                : null;
        }
    }

    public class KeyValueHelper
    {
        public static void SaveToFile(KeyValue kv, IAbsoluteFilePath fp, bool asBinary = false) {
            MainLog.Logger.Debug($"Saving KV to file {fp}, {asBinary}");
            kv.SaveToFile(kv.ToString(), asBinary);
            MainLog.Logger.Debug($"Saved KV to file {fp}, {asBinary}");
        }

        public static async Task<KeyValue> LoadFromFileAsync(IAbsoluteFilePath fp, CancellationToken cancelToken) {
            MainLog.Logger.Debug($"Loading KV from file {fp}");
            var input = await fp.ReadTextAsync(cancelToken).ConfigureAwait(false);
            MainLog.Logger.Debug($"Loaded KV from file {fp}");
            return ParseKV(input);
        }

        public static KeyValue LoadFromFile(IAbsoluteFilePath fp) {
            MainLog.Logger.Debug($"Loading KV from file {fp}");
            var input = fp.ReadAllText();
            MainLog.Logger.Debug($"Loaded KV from file {fp}");
            return ParseKV(input);
        }

        private static KeyValue ParseKV(string input) {
            MainLog.Logger.Debug($"Parsing KV");
            var v = KeyValue.LoadFromString(input);
            MainLog.Logger.Debug($"Parsed KV");
            return v;
        }
    }
    
    public class SteamHelper : ISteamHelper
    {
        readonly Dictionary<uint, SteamApp> _appCache;
        readonly IEnumerable<IAbsoluteDirectoryPath> _baseInstallPaths;
        readonly bool _cache;
        readonly IAbsoluteDirectoryPath _steamPath;

        public static SteamHelper Create()
            => new SteamHelper(new SteamStuff(SteamPathHelper.SteamPath).TryReadSteamConfig(),
                SteamPathHelper.SteamPath);


        public IAbsoluteDirectoryPath SteamPath => _steamPath;

        public SteamHelper(KeyValue steamConfig, IAbsoluteDirectoryPath steamPath, bool cache = true) {
            _cache = cache;
            _appCache = new Dictionary<uint, SteamApp>();
            KeyValues = steamConfig;
            _steamPath = steamPath;
            SteamFound = _steamPath != null && _steamPath.Exists;
            _baseInstallPaths = GetBaseInstallFolderPaths();
        }

        public KeyValue KeyValues { get; }
        public bool SteamFound { get; }

        KeyValue TryGetConfigByAppId(uint appId) {
            KeyValue apps = null;
            try {
                apps = KeyValues.GetKeyValue("Software", "Valve", "Steam", "apps");
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "Config Store Invalid");
                return null;
            }
            try {
                return apps.GetKeyValue(appId.ToString());
            } catch (Exception) {
                return null;
            }
        }

        public ISteamApp TryGetSteamAppById(uint appId, bool noCache = false) {
            if (!SteamFound)
                throw new InvalidOperationException("Unable to get Steam App, Steam was not found.");
            try {
                return GetSteamAppById(appId, noCache);
            } catch (Exception e) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(e, "Unknown Exception Attempting to get Steam App");
                return SteamApp.Default;
            }
        }

        SteamApp GetSteamAppById(uint appId, bool noCache) {
            if (!noCache && _appCache.ContainsKey(appId))
                return _appCache[appId];
            var app = new SteamApp(appId, GetAppManifestLocation(appId), TryGetConfigByAppId(appId));
            if (app.InstallBase != null)
                _appCache[appId] = app;
            return app;
        }

        IAbsoluteDirectoryPath GetAppManifestLocation(uint appId)
            => _baseInstallPaths.FirstOrDefault(installPath => CheckForAppManifest(appId, installPath));

        bool CheckForAppManifest(uint appId, IAbsoluteDirectoryPath installPath)
            => installPath.GetChildDirectoryWithName("SteamApps")
                .GetChildFileWithName("appmanifest_" + appId + ".acf")
                .Exists;

        IReadOnlyList<IAbsoluteDirectoryPath> GetBaseInstallFolderPaths() {
            var list = new List<IAbsoluteDirectoryPath> {_steamPath};
            if (KeyValues == null)
                return list.AsReadOnly();
            try {
                var kv = KeyValues.GetKeyValue("Software", "Valve", "Steam");
                var iFolder = 1;
                string key;
                while (kv.ContainsKey(key = BuildKeyName(iFolder++)))
                    list.Add(kv[key].AsString().ToAbsoluteDirectoryPath());
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "Config Store Invalid");
            }
            return list.AsReadOnly();
        }

        private static string BuildKeyName(int iFolder) => "BaseInstallFolder_" + iFolder;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class SteamApp : ISteamApp
    {
        protected SteamApp() {}

        public SteamApp(uint appId, IAbsoluteDirectoryPath installBase, KeyValue appConfig) {
            AppId = appId;
            InstallBase = installBase;
            AppConfig = appConfig;
            if (installBase != null)
                LoadManifest();
            SetAppPath();
        }

        public uint AppId { get; }
        public IAbsoluteDirectoryPath InstallBase { get; }
        public IAbsoluteDirectoryPath AppPath { get; private set; }
        public KeyValue AppManifest { get; private set; }
        public KeyValue AppConfig { get; }
        public virtual bool IsValid => true;
        public static SteamApp Default { get; } = new NullSteamApp();

        public string GetInstallDir() {
            if (AppConfig != null && AppConfig.ContainsKey("installdir"))
                return AppConfig["installdir"].AsString();
            try {
                return AppManifest?.GetKeyValue("installdir").AsString();
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "AppManifest Invalid ({0})".FormatWith(AppId));
            }
            return null;
        }

        void SetAppPath() {
            var installDir = GetInstallDir();
            AppPath = InstallBase.GetChildDirectoryWithName(@"SteamApps\Common\" + installDir);
        }

        void LoadManifest() {
            var configPath =
                InstallBase.GetChildDirectoryWithName("SteamApps").GetChildFileWithName("appmanifest_" + AppId + ".acf");
            AppManifest = KeyValueHelper.LoadFromFile(configPath);
        }

        class NullSteamApp : SteamApp
        {
            public override bool IsValid => false;
        }
    }

    public class ParseException : Exception
    {
        public ParseException() {}
        public ParseException(string message) : base(message) {}
    }
}