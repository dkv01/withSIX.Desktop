// <copyright company="SIX Networks GmbH" file="LocalMachineInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using Microsoft.Win32;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Play.Core.Games.Entities;
using SN.withSIX.Play.Core.Games.Legacy.Events;
using SN.withSIX.Play.Core.Games.Legacy.Steam;

// ReSharper disable InconsistentNaming

namespace SN.withSIX.Play.Core.Games.Legacy
{
    public static class SteamInfos
    {
        public const string SteamExecutable = "Steam.exe";
        public const string SteamServiceExecutable = "steamservice.exe";
    }

    public class LocalMachineInfo : IEnableLogging
    {
        const string ts3Registry = @"SOFTWARE\Teamspeak 3 Client";

        public LocalMachineInfo() {
            update(false);
        }

        public IAbsoluteDirectoryPath TS3_32_Path { get; private set; }
        public IAbsoluteDirectoryPath TS3_64_Path { get; private set; }
        public IAbsoluteDirectoryPath DocumentsPath { get; set; }
        public KeyValues SteamConfig { get; set; }
        public SteamHelper SteamHelper { get; set; }

        void update(bool raiseEvent = true) {
            SetPaths();

            TryReadSteamConfig();

            SteamHelper = new SteamHelper(SteamConfig, GetSteamPath());

            if (raiseEvent)
                CalculatedGameSettings.RaiseEvent(new LocalMachineInfoChanged());
        }

        void TryReadSteamConfig() {
            try {
                ReadSteamConfig();
            } catch (ParseException e) {
                this.Logger().FormattedWarnException(e, "during steam config parsing");
            }
        }

        void ReadSteamConfig() {
            var steamPath = GetSteamPath();
            if (steamPath == null || !steamPath.Exists)
                return;

            var steamConfigPath = steamPath.GetChildDirectoryWithName("config").GetChildFileWithName("config.vdf");
            if (steamConfigPath.Exists)
                SteamConfig = new KeyValues(Tools.FileUtil.Ops.ReadTextFileWithRetry(steamConfigPath));
        }

        public void Update() {
            update();
        }

        static string GetNullIfPathEmpty(string path) => String.IsNullOrWhiteSpace(path) ? null : path;

        IAbsoluteDirectoryPath TryGetDocumentsOrTempPath() {
            try {
                var env = Common.Paths.MyDocumentsPath;
                if (!Tools.FileUtil.IsValidRootedPath(env.ToString()))
                    throw new ArgumentException("my documents path can't be invalid, or UNC path");
                return env;
            } catch (Exception e) {
                this.Logger().FormattedWarnException(e);
                return Common.Paths.TempPath ?? Path.GetTempPath().ToAbsoluteDirectoryPath();
            }
        }

        void SetTs3Paths() {
            TS3_32_Path = GetUserOrLmPath(ts3Registry).ToAbsoluteDirectoryPathNullSafe();
            TS3_64_Path =
                GetUserOrLmPath(ts3Registry, String.Empty, RegistryView.Registry64).ToAbsoluteDirectoryPathNullSafe();
        }

        static string GetUserOrLmPath(string regKey, string regVal = "", RegistryView view = RegistryView.Registry32) {
            var path = GetUserOrLmPathInternal(regKey, regVal, view);
            return !Tools.FileUtil.IsValidRootedPath(path) ? null : Tools.FileUtil.CleanPath(path);
        }

        static string GetUserOrLmPathInternal(string regKey, string regVal, RegistryView view) => GetNullIfPathEmpty(Tools.Generic.NullSafeGetRegKeyValue<string>(regKey, regVal, view,
    RegistryHive.CurrentUser))
       ?? GetNullIfPathEmpty(Tools.Generic.NullSafeGetRegKeyValue<string>(regKey, regVal, view));

        void SetPaths() {
            SetTs3Paths();
            DocumentsPath = TryGetDocumentsOrTempPath();
        }

        public static IAbsoluteDirectoryPath GetSteamPath() {
            var steamDirectory = DomainEvilGlobal.Settings.GameOptions.SteamDirectory;
            return steamDirectory == null || !steamDirectory.IsValidAbsoluteDirectoryPath()
                ? DefaultSteamPath
                : steamDirectory.ToAbsoluteDirectoryPath();
        }

        public static IAbsoluteDirectoryPath DefaultSteamPath => SteamPathHelper.SteamPath;

        class SteamPathHelper
        {
            private static readonly string steamRegistry = @"SOFTWARE\Valve\Steam";
            private static IAbsoluteDirectoryPath _steamPath;
            public static IAbsoluteDirectoryPath SteamPath => _steamPath ?? (_steamPath = GetSteamPathInternal());

            private static IAbsoluteDirectoryPath GetSteamPathInternal() {
                var regPath = TryGetPathFromRegistry();
                if (regPath != null && regPath.Exists)
                    return regPath;
                var expectedPath =
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                        .ToAbsoluteDirectoryPath()
                        .GetChildDirectoryWithName("Steam");
                return expectedPath.Exists ? expectedPath : null;
            }

            private static IAbsoluteDirectoryPath TryGetPathFromRegistry() {
                var p = Tools.Generic.NullSafeGetRegKeyValue<string>(steamRegistry, "InstallPath");
                return p.IsBlankOrWhiteSpace() ? null : p.Trim().ToAbsoluteDirectoryPath();
            }
        }
    }
}