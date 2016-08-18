// <copyright company="SIX Networks GmbH" file="SteamHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Steam.Core.SteamKit.Utils;
using SteamKit2;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Steam.Core
{
    public class SteamStuff
    {
        public KeyValue TryReadSteamConfig() {
            try {
                return ReadSteamConfig();
            } catch (ParseException ex) {
                //this.Logger().FormattedFormattedWarnException(ex, "during steam config parsing");
                return null;
            }
        }

        KeyValue ReadSteamConfig() {
            var steamPath = GetSteamPath();
            if (steamPath == null || !steamPath.Exists)
                return null;

            var steamConfigPath = steamPath.GetChildDirectoryWithName("config").GetChildFileWithName("config.vdf");
            if (steamConfigPath.Exists)
                return KeyValue.LoadFromString(Tools.FileUtil.Ops.ReadTextFileWithRetry(steamConfigPath));

            return null;
        }

        public static IAbsoluteDirectoryPath GetSteamPath() => SteamPathHelper.GetSteamPath();
    }

    public class SteamHelper
    {
        readonly Dictionary<uint, SteamApp> _appCache;
        readonly IEnumerable<IAbsoluteDirectoryPath> _baseInstallPaths;
        readonly bool _cache;
        readonly IAbsoluteDirectoryPath _steamPath;

        public SteamHelper(KeyValue steamConfig, IAbsoluteDirectoryPath steamPath, bool cache = true) {
            _cache = cache;
            _appCache = new Dictionary<uint, SteamApp>();
            KeyValues = steamConfig;
            _steamPath = steamPath;
            SteamFound = KeyValues != null || (_steamPath != null && _steamPath.Exists);
            _baseInstallPaths = GetBaseInstallFolderPaths();
        }

        public KeyValue KeyValues { get; }
        public bool SteamFound { get; }

        KeyValue TryGetConfigByAppId(uint appId) {
            KeyValue apps = null;
            try {
                apps = KeyValues.GetKeyValue("InstallConfigStore", "Software", "Valve", "Steam", "apps");
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

        public SteamApp TryGetSteamAppById(uint appId, bool noCache = false) {
            if (!SteamFound)
                throw new NotFoundException("Unable to get Steam App, Steam was not found.");
            try {
                return GetSteamAppById(appId, noCache);
            } catch (Exception e) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(e, "Unknown Exception Attempting to get Steam App");
                return SteamApp.Default;
            }
        }

        SteamApp GetSteamAppById(uint appId, bool noCache) {
            SteamApp app = null;
            if (noCache || !_appCache.ContainsKey(appId)) {
                app = new SteamApp(appId, GetAppManifestLocation(appId), TryGetConfigByAppId(appId));
                if (app.InstallBase != null)
                    _appCache[appId] = app;
            } else
                app = _appCache[appId];
            return app;
        }

        IAbsoluteDirectoryPath GetAppManifestLocation(uint appId)
            => _baseInstallPaths.FirstOrDefault(installPath => CheckForAppManifest(appId, installPath));

        bool CheckForAppManifest(uint appId, IAbsoluteDirectoryPath installPath)
            => installPath.GetChildDirectoryWithName("SteamApps")
                .GetChildFileWithName("appmanifest_" + appId + ".acf")
                .Exists;

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        IReadOnlyList<IAbsoluteDirectoryPath> GetBaseInstallFolderPaths() {
            var list = new List<IAbsoluteDirectoryPath>();
            list.Add(_steamPath);
            if (KeyValues == null)
                return list.AsReadOnly();
            try {
                var kv = KeyValues.GetKeyValue("InstallConfigStore", "Software", "Valve", "Steam");
                var iFolder = 1;
                var key = "BaseInstallFolder_" + iFolder;
                while (kv.ContainsKey(key)) {
                    list.Add(kv[key].AsString().ToAbsoluteDirectoryPath());
                    iFolder++;
                }
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "Config Store Invalid");
            }
            return list.AsReadOnly();
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class SteamApp
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
                return AppManifest?.GetKeyValue("AppState", "installdir").AsString();
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "AppManifest Invalid ({0})".FormatWith(AppId));
            }
            return null;
        }

        void SetAppPath() {
            var installDir = GetInstallDir();
            if (installDir == null)
                return;
            AppPath = InstallBase.GetChildDirectoryWithName(@"SteamApps\Common\" + installDir);
        }

        void LoadManifest() {
            var configPath =
                InstallBase.GetChildDirectoryWithName("SteamApps").GetChildFileWithName("appmanifest_" + AppId + ".acf");
            AppManifest = KeyValue.LoadFromString(Tools.FileUtil.Ops.ReadTextFileWithRetry(configPath));
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