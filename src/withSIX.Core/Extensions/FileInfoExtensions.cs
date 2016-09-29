// <copyright company="SIX Networks GmbH" file="FileInfoExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using NDepend.Path;

namespace withSIX.Core.Extensions
{
    public static class FileInfoExtensions
    {
        public static bool IsUncPath(this FileInfo info) {
            Uri uri = null;
            if (!Uri.TryCreate(info.FullName, UriKind.Absolute, out uri))
                return false;
            return uri.IsUnc;
        }

        public static bool IsUncPath(this DirectoryInfo info) {
            Uri uri = null;
            if (!Uri.TryCreate(info.FullName, UriKind.Absolute, out uri))
                return false;
            return uri.IsUnc;
        }

        public static IAbsoluteFilePath ToAbsoluteFilePath(this FileInfo This) => This.FullName.ToAbsoluteFilePath();

        public static IAbsoluteDirectoryPath ToAbsoluteDirectoryPath(this DirectoryInfo This)
            => This.FullName.ToAbsoluteDirectoryPath();
    }
}