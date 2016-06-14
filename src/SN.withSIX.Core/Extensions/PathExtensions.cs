// <copyright company="SIX Networks GmbH" file="PathExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NDepend.Path;

namespace SN.withSIX.Core.Extensions
{
    public static class PathExtensions
    {
        static readonly Regex RxDrive = new Regex(@"^([a-z]):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string EscapePath(this string arg) => $"\"{arg}\"";

        public static IRelativeDirectoryPath GetRoot(this IRelativeDirectoryPath path) {
            var dir = path;
            while (dir.HasParentDirectory)
                dir = path.ParentDirectoryPath;
            return dir;
        }

        public static bool MatchesSub(this IRelativeFilePath path, string name) => path.ParentDirectoryPath.MatchesSub(name);

        public static bool MatchesSub(this IRelativeDirectoryPath path, string name) {
            var dir = path;
            if (dir.DirectoryName.Equals(name))
                return true;
            while (dir.HasParentDirectory) {
                dir = path.ParentDirectoryPath;
                if (dir.DirectoryName.Equals(name))
                    return true;
            }
            return false;
        }

        public static IAbsoluteDirectoryPath GetRoot(this IAbsoluteDirectoryPath path) {
            var dir = path;
            while (dir.HasParentDirectory)
                dir = path.ParentDirectoryPath;
            return dir;
        }

        public static IRelativeDirectoryPath GetRoot(this IRelativeFilePath path) => path.ParentDirectoryPath.GetRoot();

        public static IAbsoluteDirectoryPath GetNearestExisting(this IAbsoluteDirectoryPath path) {
            if (path.Exists)
                return path;
            while (path.HasParentDirectory) {
                path = path.ParentDirectoryPath;
                if (path.Exists)
                    return path;
            }
            return path;
        }

        public static IAbsoluteDirectoryPath ToAbsoluteDirectoryPathNullSafe(this string path)
            => string.IsNullOrEmpty(path) ? null : path.ToAbsoluteDirectoryPath();

        public static IAbsoluteFilePath ToAbsoluteFilePathNullSafe(this string path)
            => string.IsNullOrEmpty(path) ? null : path.ToAbsoluteFilePath();

        public static string EscapePath(this IAbsolutePath arg) => $"\"{arg}\"";

        public static IEnumerable<DirectoryInfo> FilterDotted(this IEnumerable<DirectoryInfo> infos)
            => infos.Where(x => !StartsWithDot(x));

        public static IEnumerable<FileInfo> FilterDotted(this IEnumerable<FileInfo> infos)
            => infos.Where(x => !StartsWithDot(x));

        static bool StartsWithDot(FileSystemInfo x) => x.Name.StartsWith(".");

        public static IEnumerable<DirectoryInfo> RecurseFilterDottedDirectories(this DirectoryInfo di) {
            var filterDottedDirectories = di.FilterDottedDirectories();
            return filterDottedDirectories;
            //.Concat(filterDottedDirectories.SelectMany(RecurseFilterDottedDirectories));
        }

        public static IEnumerable<DirectoryInfo> FilterDottedDirectories(this DirectoryInfo di)
            => di.EnumerateDirectories().FilterDotted();

        public static IEnumerable<FileInfo> FilterDottedFiles(this DirectoryInfo di)
            => di.EnumerateFiles().FilterDotted();

        public static string CleanPath(this string path) => path.Replace("/", "\\").TrimEnd('\\');

        public static string CygwinPath(this string arg) {
            arg = PosixSlash(arg);
            var match = RxDrive.Match(arg);
            return match.Success ? arg.Replace(match.Value, "/cygdrive/" + match.Groups[1].Value.ToLower()) : arg;
        }

        public static string MingwPath(this string arg) {
            arg = PosixSlash(arg);
            var match = RxDrive.Match(arg);
            return match.Success ? arg.Replace(match.Value, "/" + match.Groups[1].Value.ToLower()) : arg;
        }

        public static string PosixSlash(this string arg) => arg.Replace('\\', '/');
    }
}