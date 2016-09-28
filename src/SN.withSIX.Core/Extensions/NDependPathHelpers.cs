// <copyright company="SIX Networks GmbH" file="NDependPathHelpers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Helpers;

namespace SN.withSIX.Core.Extensions
{
    public static class NDependPathHelpers
    {
        private static readonly string[] archiveExts = {"gz", "7z", "rar", "zip"};
        private static readonly string[] archiveExtensions = archiveExts.Select(x => $".{x}").ToArray();
        public static readonly Regex ArchiveRx = new Regex(@"\.(" + string.Join("|", archiveExts) + ")");
        // TODO: Why not use FileInfo/DirectoryInfo?
        public static void Copy(this IAbsoluteFilePath src, IAbsoluteFilePath destination, bool overwrite = true,
            bool checkMd5 = false) => Tools.FileUtil.Ops.Copy(src, destination, overwrite, checkMd5);

        public static void Move(this IAbsoluteFilePath src, IAbsoluteFilePath destination, bool overwrite = true,
            bool checkMd5 = false) => Tools.FileUtil.Ops.Move(src, destination, overwrite, checkMd5);

        public static void Move(this IAbsoluteFilePath src, IAbsoluteDirectoryPath destination, bool overwrite = true,
                bool checkMd5 = false)
            => src.Move(destination.GetChildFileWithName(src.FileName), overwrite, checkMd5);

        public static IEnumerable<IRelativeFilePath> ToRelativeFilePaths(this IEnumerable<string> files)
            => files.Select(ToRelativeFile);

        static IRelativeFilePath ToRelativeFile(this string file) => $@".\{file}".ToRelativeFilePath();

        public static IEnumerable<IRelativeDirectoryPath> ToRelativeDirectoryPaths(this IEnumerable<string> directories)
            => directories.Select(ToRelativeDirectory);

        static IRelativeDirectoryPath ToRelativeDirectory(this string directory)
            => $@".\{directory}".ToRelativeDirectoryPath();

        public static bool IsArchive(this IFilePath absoluteFilePath)
            => archiveExtensions.Contains(absoluteFilePath.FileExtension);

        public static void Copy(this IAbsoluteFilePath src, IAbsoluteDirectoryPath destination, bool overwrite = true,
                bool checkMd5 = false)
            => Tools.FileUtil.Ops.Copy(src, destination.GetChildFileWithName(src.FileName), overwrite, checkMd5);

        public static Task CopyAsync(this IAbsoluteFilePath src, IAbsoluteDirectoryPath destination,
                bool overwrite = true,
                bool checkMd5 = false, ITProgress status = null)
            =>
            Tools.FileUtil.Ops.CopyAsync(src, destination.GetChildFileWithName(src.FileName), overwrite, checkMd5,
                status);

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
    }
}