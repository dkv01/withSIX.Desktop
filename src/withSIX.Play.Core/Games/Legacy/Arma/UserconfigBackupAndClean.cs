﻿// <copyright company="SIX Networks GmbH" file="UserconfigBackupAndClean.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.ComponentModel;
using System.IO;
using NDepend.Path;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;

namespace withSIX.Play.Core.Games.Legacy.Arma
{
    public class UserconfigBackupAndClean : UserconfigProcessorBase
    {
        public void ProcessBackupAndCleanInstall(string path, IAbsoluteDirectoryPath gamePath,
            IAbsoluteDirectoryPath uconfig, IAbsoluteDirectoryPath uconfigPath) {
            BackupExistingUserconfig(uconfigPath.ToString());
            TryUserconfigClean(path, gamePath, uconfig, uconfigPath);
        }

        static void BackupExistingUserconfig(string uconfigPath) {
            var time = Tools.Generic.GetCurrentUtcDateTime.Ticks;
            if (Directory.Exists(uconfigPath))
                Directory.Move(uconfigPath, uconfigPath + "_" + time);
            if (File.Exists(uconfigPath))
                File.Move(uconfigPath, uconfigPath + "_" + time);
        }

        void TryUserconfigClean(string path, IAbsoluteDirectoryPath gamePath, IAbsoluteDirectoryPath uconfig,
            IAbsoluteDirectoryPath uconfigPath) {
            if (!ConfirmUserconfigIsNotFile(uconfig.ToString()))
                return;

            if (Directory.Exists(path))
                TryUserconfigDirectoryOverwrite(path.ToAbsoluteDirectoryPath(), uconfigPath);
            else
                TryUserconfigUnpackOverwrite(path.ToAbsoluteFilePath(), gamePath);
        }

        static void TryUserconfigUnpackOverwrite(IAbsoluteFilePath path, IAbsoluteDirectoryPath gamePath) {
            try {
                Tools.Compression.UnpackRetryUpdater(path, gamePath, true);
            } catch (Win32Exception ex) {
                if (ex.NativeErrorCode != Win32ErrorCodes.ERROR_CANCELLED_ELEVATION)
                    throw;
                throw ex.HandleUserCancelled();
            }
        }

        static void TryUserconfigDirectoryOverwrite(IAbsoluteDirectoryPath path, IAbsoluteDirectoryPath uconfigPath) {
            try {
                Tools.FileUtil.Ops.CopyDirectoryWithUpdaterFallbackAndRetry(path, uconfigPath, true);
            } catch (Win32Exception ex) {
                if (ex.NativeErrorCode != Win32ErrorCodes.ERROR_CANCELLED_ELEVATION)
                    throw;
                throw ex.HandleUserCancelled();
            }
        }
    }
}