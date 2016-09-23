// <copyright company="SIX Networks GmbH" file="GameFolderService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using NDepend.Path;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.ContentEngine.Infra.Services
{
    public interface IGameFolderService
    {
        void InstallDllPlugin(IContentEngineGame game, IContentEngineContent content, string plugin, bool force = false);
    }

    public class GameFolderService : IGameFolderService
    {
        public void InstallDllPlugin(IContentEngineGame game, IContentEngineContent content, string plugin, bool force = false) {
            var success = TryInstallPlugin(game.WorkingDirectory, content, plugin, force);

            MainLog.Logger.Info("Install Success?: " + success);
        }

        bool TryInstallPlugin(IAbsoluteDirectoryPath gamePath, IContentEngineContent mod, string plugin, bool force) {
            try {
                return InstallPlugin(gamePath, mod, plugin, force);
            } catch (PathDoesntExistException e) {
                MainLog.Logger.FormattedWarnException(e, "Path: " + e.Path);
            } catch (Win32Exception e) {
                MainLog.Logger.FormattedWarnException(e);
            } catch (Exception ex) {
                MainLog.Logger.FormattedErrorException(ex);
            }
            return false;
        }

        bool InstallPlugin(IAbsoluteDirectoryPath gamePath, IContentEngineContent mod, string plugin, bool force) {
            Contract.Requires<ArgumentNullException>(gamePath.IsNotNullAndExists(),
                "Unable to find the Game Install Directory");
            Contract.Requires<ArgumentNullException>(mod != null, "Fatal Error Occured: Mod incorrectly registered");
            Contract.Requires<ArgumentNullException>(plugin != null, "Fatal Error Occured: Plugin Path was not set");

            if (!mod.IsInstalled || !mod.PathInternal.IsNotNullAndExists())
                throw new InvalidOperationException("The mod is not installed");

            var pluginPath = mod.PathInternal.GetChildFileWithName(plugin);
            var gamePluginFolder = gamePath;

            return InstallDll(pluginPath, gamePluginFolder, force);
        }

        static bool InstallDll(IAbsoluteFilePath pluginPath, IAbsoluteDirectoryPath gamePluginFolder,
            bool force = true) {
            Contract.Requires<ArgumentNullException>(gamePluginFolder != null);
            Contract.Requires<ArgumentNullException>(pluginPath != null);

            if (!pluginPath.IsNotNullAndExists())
                throw new PathDoesntExistException(pluginPath.ToString());

            if (!gamePluginFolder.IsNotNullAndExists())
                throw new PathDoesntExistException(gamePluginFolder.ToString());

            var fullPath = gamePluginFolder.GetChildFileWithName(pluginPath.FileName);

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
    }
}