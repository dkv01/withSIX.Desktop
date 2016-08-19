// <copyright company="SIX Networks GmbH" file="NDependPathHelpers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Helpers;

namespace SN.withSIX.Core.Extensions
{
    public static class NDependPathHelpers
    {
        // TODO: Why not use FileInfo/DirectoryInfo?
        public static void Copy(this IAbsoluteFilePath src, IAbsoluteFilePath destination, bool overwrite = true,
            bool checkMd5 = false) => Tools.FileUtil.Ops.Copy(src, destination, overwrite, checkMd5);

        public static void Move(this IAbsoluteFilePath src, IAbsoluteFilePath destination, bool overwrite = true,
            bool checkMd5 = false) => Tools.FileUtil.Ops.Move(src, destination, overwrite, checkMd5);

        public static void Copy(this IAbsoluteFilePath src, IAbsoluteDirectoryPath destination, bool overwrite = true,
            bool checkMd5 = false)
            => Tools.FileUtil.Ops.Copy(src, destination.GetChildFileWithName(src.FileName), overwrite, checkMd5);

        public static Task CopyAsync(this IAbsoluteFilePath src, IAbsoluteDirectoryPath destination,
            bool overwrite = true,
            bool checkMd5 = false, ITProgress status = null)
            => Tools.FileUtil.Ops.CopyAsync(src, destination.GetChildFileWithName(src.FileName), overwrite, checkMd5, status);

        public static Task CopyAsync(this IAbsoluteFilePath src, IAbsoluteFilePath destination,
            bool overwrite = true,
            bool checkMd5 = false, ITProgress status = null)
            => Tools.FileUtil.Ops.CopyAsync(src, destination, overwrite, checkMd5, status);

        public static void Copy(this IAbsoluteDirectoryPath src, IAbsoluteDirectoryPath destination,
            bool overwrite = false,
            bool checkMd5 = false) => Tools.FileUtil.Ops.CopyDirectory(src, destination, overwrite);

        public static void Delete(this IAbsoluteFilePath src) => Tools.FileUtil.Ops.DeleteFile(src);

        public static void Delete(this IAbsolutePath src, bool recursive = false) {
            if (src.IsFilePath)
                ((IAbsoluteFilePath) src).Delete();
            else
                global::withSIX.Api.Models.Extensions.FileExt.Delete((IAbsoluteDirectoryPath) src, recursive);
        }

        public static bool IsEmpty(this IAbsoluteDirectoryPath path)
            => !path.Exists || !path.DirectoryInfo.EnumerateFileSystemInfos().Any();

        public static bool IsEmptySafe(this IAbsoluteDirectoryPath path) {
            try {
                return path.IsEmpty();
            } catch (UnauthorizedAccessException) {
                return true;
            } catch (IOException) {
                return true;
            }
        }

        public static void Unpack(this IAbsoluteFilePath src, IAbsoluteDirectoryPath outputFolder,
            bool overwrite = false, bool fullPath = true, bool checkFileIntegrity = true, ITProgress progress = null)
            => Tools.Compression.Unpack(src, outputFolder, overwrite, fullPath, checkFileIntegrity, progress);

        public static T LoadXml<T>(this IAbsoluteFilePath src)
            => Tools.Serialization.Xml.LoadXmlFromFile<T>(src.ToString());
    }
}