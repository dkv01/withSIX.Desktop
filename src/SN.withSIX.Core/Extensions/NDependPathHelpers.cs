// <copyright company="SIX Networks GmbH" file="NDependPathHelpers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public static void Move(this IAbsoluteFilePath src, IAbsoluteDirectoryPath destination, bool overwrite = true,
            bool checkMd5 = false)
            => src.Move(destination.GetChildFileWithName(src.FileName), overwrite, checkMd5);

        private static readonly string[] archiveExts = { "gz", "7z", "rar", "zip" };
        private static readonly string[] archiveExtensions = archiveExts.Select(x => $".{x}").ToArray();
        public static readonly Regex ArchiveRx = new Regex(@"\.(" + string.Join("|", archiveExts) + ")");


        public static bool IsArchive(this IFilePath absoluteFilePath)
            => archiveExtensions.Contains(absoluteFilePath.FileExtension);

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

        public static void Unpack(this IAbsoluteFilePath src, IAbsoluteDirectoryPath outputFolder,
            bool overwrite = false, bool fullPath = true, bool checkFileIntegrity = true, ITProgress progress = null)
            => Tools.Compression.Unpack(src, outputFolder, overwrite, fullPath, checkFileIntegrity, progress);

        public static T LoadXml<T>(this IAbsoluteFilePath src)
            => Tools.Serialization.Xml.LoadXmlFromFile<T>(src.ToString());
    }
}