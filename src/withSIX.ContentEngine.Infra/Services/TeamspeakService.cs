// <copyright company="SIX Networks GmbH" file="TeamspeakService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.Win32;
using NDepend.Path;
using withSIX.ContentEngine.Core;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;

namespace withSIX.ContentEngine.Infra.Services
{
    public interface ITeamspeakService
    {
        bool IsX86Installed();
        bool IsX64Installed();
        void InstallX86Plugin(IContentEngineContent content, string plugin, bool force = false);
        void InstallX64Plugin(IContentEngineContent content, string plugin, bool force = false);
        void InstallX86PluginFolder(IContentEngineContent content, string plugin, bool force = false);
        void InstallX64PluginFolder(IContentEngineContent content, string plugin, bool force = false);
    }

    public class TeamspeakService : ITeamspeakService
    {
        const string Ts3SubPath = "plugins";
        const string ts3Registry = @"SOFTWARE\Teamspeak 3 Client";
        readonly IAbsoluteDirectoryPath TS3_32_Path;
        readonly IAbsoluteDirectoryPath TS3_64_Path;

        public TeamspeakService() {
            TS3_32_Path = GetUserOrLmPath(ts3Registry).ToAbsoluteDirectoryPathNullSafe();
            TS3_64_Path =
                GetUserOrLmPath(ts3Registry, string.Empty, RegistryView.Registry64).ToAbsoluteDirectoryPathNullSafe();
        }

        public bool IsX86Installed() => TS3_32_Path.IsNotNullAndExists();

        public bool IsX64Installed() => TS3_64_Path.IsNotNullAndExists();

        public void InstallX86Plugin(IContentEngineContent content, string plugin, bool force = false) {
            var success = TryInstallPlugin(TS3_32_Path, content, plugin, force);

            MainLog.Logger.Info("Install Success?: " + success);
        }

        public void InstallX64Plugin(IContentEngineContent content, string plugin, bool force = false) {
            var success = TryInstallPlugin(TS3_64_Path, content, plugin, force);

            MainLog.Logger.Info("Install Success?: " + success);
        }

        public void InstallX86PluginFolder(IContentEngineContent content, string plugin, bool force = false) {
            var success = TryInstallPluginFolder(TS3_32_Path, content, plugin, force);

            MainLog.Logger.Info("Install Success?: " + success);
        }

        public void InstallX64PluginFolder(IContentEngineContent content, string plugin, bool force = false) {
            var success = TryInstallPluginFolder(TS3_64_Path, content, plugin, force);

            MainLog.Logger.Info("Install Success?: " + success);
        }

        static string GetUserOrLmPath(string regKey, string regVal = "", RegistryView view = RegistryView.Registry32) {
            var path = GetUserOrLmPathInternal(regKey, regVal, view);
            return !Tools.FileUtil.IsValidRootedPath(path) ? null : Tools.FileUtil.CleanPath(path);
        }

        static string GetUserOrLmPathInternal(string regKey, string regVal, RegistryView view)
            => GetNullIfPathEmpty(Tools.Generic.NullSafeGetRegKeyValue<string>(regKey, regVal, view,
                   RegistryHive.CurrentUser))
               ?? GetNullIfPathEmpty(Tools.Generic.NullSafeGetRegKeyValue<string>(regKey, regVal, view));

        static string GetNullIfPathEmpty(string path) => string.IsNullOrWhiteSpace(path) ? null : path;

        bool TryInstallPlugin(IAbsoluteDirectoryPath tsPath, IContentEngineContent mod, string plugin, bool force) {
            try {
                return InstallPlugin(tsPath, mod, plugin, force);
            } catch (PathDoesntExistException e) {
                MainLog.Logger.FormattedWarnException(e, "Path: " + e.Path);
            } catch (Win32Exception e) {
                MainLog.Logger.FormattedWarnException(e);
            } catch (Exception ex) {
                MainLog.Logger.FormattedErrorException(ex);
            }
            return false;
        }

        bool TryInstallPluginFolder(IAbsoluteDirectoryPath tsPath, IContentEngineContent mod, string plugin, bool force) {
            try {
                return InstallPluginFolder(tsPath, mod, plugin, force);
            } catch (PathDoesntExistException e) {
                MainLog.Logger.FormattedWarnException(e, "Path: " + e.Path);
            } catch (Win32Exception e) {
                MainLog.Logger.FormattedWarnException(e);
            } catch (Exception ex) {
                MainLog.Logger.FormattedErrorException(ex);
            }
            return false;
        }

        bool InstallPluginFolder(IAbsoluteDirectoryPath tsPath, IContentEngineContent mod, string plugin, bool force) {
            if (!tsPath.IsNotNullAndExists())
                throw new ArgumentNullException("Unable to find the Teamspeak Install Directory");

            if (mod == null) throw new ArgumentNullException(nameof(mod), "Fatal Error Occured: Mod incorrectly registered");
            if (plugin == null) throw new ArgumentNullException(nameof(plugin), "Fatal Error Occured: Plugin Path was not set");

            if (!mod.IsInstalled || !mod.PathInternal.IsNotNullAndExists())
                throw new InvalidOperationException("The mod is not installed");

            var pluginPath = Path.Combine(mod.PathInternal.ToString(), plugin).ToAbsoluteDirectoryPath();
            var tsPluginFolder = GetPluginPath(tsPath);

            InstallFolder(pluginPath, tsPluginFolder, force);
            return true;
        }

        bool InstallPlugin(IAbsoluteDirectoryPath tsPath, IContentEngineContent mod, string plugin, bool force) {
            if (!tsPath.IsNotNullAndExists())
                throw new ArgumentNullException("Unable to find the Teamspeak Install Directory");

            if (mod == null) throw new ArgumentNullException(nameof(mod), "Fatal Error Occured: Mod incorrectly registered");
            if (plugin == null) throw new ArgumentNullException(nameof(plugin), "Fatal Error Occured: Plugin Path was not set");

            if (!mod.IsInstalled || !mod.PathInternal.IsNotNullAndExists())
                throw new InvalidOperationException("The mod is not installed");

            var pluginPath = Path.Combine(mod.PathInternal.ToString(), plugin).ToAbsoluteFilePath();
            var tsPluginFolder = GetPluginPath(tsPath);

            return InstallDll(pluginPath, tsPluginFolder, force);
        }

        static bool InstallDll(IAbsoluteFilePath pluginPath, IAbsoluteDirectoryPath tsPluginFolder,
            bool force = true) {
            if (tsPluginFolder == null) throw new ArgumentNullException(nameof(tsPluginFolder));
            if (pluginPath == null) throw new ArgumentNullException(nameof(pluginPath));

            if (!pluginPath.IsNotNullAndExists())
                throw new PathDoesntExistException(pluginPath.ToString());

            if (!tsPluginFolder.IsNotNullAndExists())
                throw new PathDoesntExistException(tsPluginFolder.ToString());

            var fullPath = tsPluginFolder.GetChildFileWithName(pluginPath.FileName);

            if (!force && fullPath.Exists)
                return false;

            return TryCopyDll(pluginPath, fullPath);
        }

        static bool TryCopyDll(IAbsoluteFilePath fi, IAbsoluteFilePath fullPath) {
            try {
                Tools.FileUtil.Ops.CopyWithUpdaterFallbackAndRetry(fi, fullPath, true, true);
                return true;
            } catch (Win32Exception ex) {
                if (ex.IsElevationCancelled())
                    throw ex.HandleUserCancelled();
                throw;
            }
        }

        static void InstallFolder(IAbsoluteDirectoryPath pluginPath, IAbsoluteDirectoryPath tsPluginFolder, bool force) {
            if (tsPluginFolder == null) throw new ArgumentNullException(nameof(tsPluginFolder));
            if (pluginPath == null) throw new ArgumentNullException(nameof(pluginPath));

            if (!pluginPath.IsNotNullAndExists())
                throw new PathDoesntExistException(pluginPath.ToString());

            if (!tsPluginFolder.IsNotNullAndExists())
                throw new PathDoesntExistException(tsPluginFolder.ToString());

            TryCopyFolder(pluginPath, tsPluginFolder.GetChildDirectoryWithName(pluginPath.DirectoryName), force);
        }

        static void TryCopyFolder(IAbsoluteDirectoryPath di, IAbsoluteDirectoryPath fullPath, bool force) {
            try {
                Tools.FileUtil.Ops.CopyDirectoryWithUpdaterFallbackAndRetry(di, fullPath, force);
            } catch (Win32Exception ex) {
                if (ex.IsElevationCancelled())
                    throw ex.HandleUserCancelled();
                throw;
            }
        }

        static IAbsoluteDirectoryPath GetPluginPath(IAbsoluteDirectoryPath tsPath)
            => tsPath.GetChildDirectoryWithName(Ts3SubPath);
    }
}