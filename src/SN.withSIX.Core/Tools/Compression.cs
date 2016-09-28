// <copyright company="SIX Networks GmbH" file="Compression.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.IO;
using NDepend.Path;
using SN.withSIX.Core.Helpers;

namespace SN.withSIX.Core
{
    public static partial class Tools
    {
        public static CompressionTools Compression = new CompressionTools();

        #region Nested type: Compression

        public class CompressionTools
        {
            public GzipTools Gzip = new GzipTools();

            public virtual void Unpack(IAbsoluteFilePath sourceFile, IAbsoluteDirectoryPath outputFolder,
                    bool overwrite = false, bool fullPath = true, bool checkFileIntegrity = true,
                    ITProgress progress = null)
                =>
                _compressionUtil.UnpackInternal(sourceFile, outputFolder, overwrite, fullPath, checkFileIntegrity,
                    progress);

            public void PackTar(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile)
                => _compressionUtil.CreateTar(directory, outputFile);

            public void PackZip(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile)
                => _compressionUtil.CreateZip(directory, outputFile);

            public void UnpackSingle(string sourceFile, string destFile)
                => _compressionUtil.UnpackSingleInternal(sourceFile, destFile);

            public void UnpackSingle(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destFile)
                => UnpackSingle(sourceFile.ToString(), destFile.ToString());


            public virtual void UnpackRetryUpdater(IAbsoluteFilePath sourceFile, IAbsoluteDirectoryPath outputFolder,
                bool overwrite = false, bool fullPath = true) {
                Contract.Requires<ArgumentNullException>(sourceFile != null);
                Contract.Requires<ArgumentNullException>(outputFolder != null);

                FileUtil.Ops.AddIORetryDialog(() => {
                    try {
                        Unpack(sourceFile, outputFolder, overwrite, fullPath);
                    } catch (UnauthorizedAccessException) {
                        if (!UacHelper.CheckUac())
                            throw;
                        UnpackUpdater(sourceFile, outputFolder, overwrite, fullPath);
                    } catch (IOException) {
                        if (!UacHelper.CheckUac())
                            throw;
                        UnpackUpdater(sourceFile, outputFolder, overwrite, fullPath);
                    }
                });
            }

            public virtual void UnpackUpdater(IAbsoluteFilePath sourceFile, IAbsoluteDirectoryPath outputFolder,
                bool overwrite = false, bool fullPath = true) {
                Contract.Requires<ArgumentNullException>(sourceFile != null);
                Contract.Requires<ArgumentNullException>(outputFolder != null);

                Generic.RunUpdater(UpdaterCommands.Unpack, sourceFile.ToString(), outputFolder.ToString(),
                    overwrite ? "--overwrite" : null,
                    fullPath.ToString());
            }
        }

        #endregion
    }
}