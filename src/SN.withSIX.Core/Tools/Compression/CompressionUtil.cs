// <copyright company="SIX Networks GmbH" file="CompressionUtil.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using NDepend.Path;
using SN.withSIX.Core.Helpers;

namespace SN.withSIX.Core
{
    public interface ICompressionUtil {
        void UnpackInternal(IAbsoluteFilePath sourceFile, IAbsoluteDirectoryPath outputFolder,
            bool overwrite = false, bool fullPath = true, bool checkFileIntegrity = true,
            ITProgress progress = null);

        void CreateTar(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile);
        void CreateZip(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile);
        void UnpackSingleInternal(string sourceFile, string destFile);
        void UnpackGzip(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destFile, ITProgress progress = null);
    }
}