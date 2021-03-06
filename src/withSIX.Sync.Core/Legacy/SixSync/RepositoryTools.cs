// <copyright company="SIX Networks GmbH" file="RepositoryTools.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Repositories.Internals;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Sync.Core.Legacy.SixSync
{
    public class RepositoryTools : IEnableLogging
    {
        public virtual Dictionary<string, string> DowncaseDictionary(Dictionary<string, string> dict) {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            return dict.GroupBy(x => x.Key.ToLower()).Select(x => x.First())
                .ToDictionary(x => x.Key.ToLower(), x => x.Value);
        }

        public virtual void UnpackFile(IAbsoluteFilePath srcFile, IAbsoluteFilePath dstFile, IStatus status = null) {
            var dstPath = dstFile.ParentDirectoryPath;
            dstPath.MakeSurePathExists();
            dstFile.RemoveReadonlyWhenExists();

            Tools.Compression.Unpack(srcFile, dstPath, true, progress: status);
        }

        public virtual void Pack(IAbsoluteFilePath file, IAbsoluteFilePath dest = null,
            string archiveFormat = Repository.DefaultArchiveFormat) {
            if (dest == null)
                dest = (file + archiveFormat).ToAbsoluteFilePath();
            dest.ParentDirectoryPath.MakeSurePathExists();
            dest.RemoveReadonlyWhenExists();

            if (archiveFormat == Repository.DefaultArchiveFormat)
                Tools.Compression.Gzip.GzipAuto(file, dest);
            else
                throw new NotSupportedException($"{archiveFormat} is not supported, use .gz");
            //Tools.Compression.PackSevenZipNative(file, dest);
        }

        public virtual string TryGetChecksum(IAbsoluteFilePath file, string change = null) {
            if (file == null) throw new ArgumentNullException(nameof(file));

            try {
                return Tools.HashEncryption.MD5FileHash(file);
            } catch (Exception e) {
                if (change == null)
                    change = file.FileName;
                throw new ChecksumException($"Checksum error for {change}.", e);
            }
        }

        public virtual string GetGuid(string path) {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var repo = Path.Combine(path, Repository.VersionFileName).ToAbsoluteFilePath();
            return SyncEvilGlobal.Yaml.NewFromYamlFile<RepoVersion>(repo).Guid;
        }

        public virtual string TryGetGuid(string path) {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return !File.Exists(Path.Combine(path, Repository.VersionFileName)) ? null : TryGetGuid2(path);
        }

        string TryGetGuid2(string path) {
            try {
                return GetGuid(path);
            } catch (Exception e) {
                this.Logger().FormattedWarnException(e);
                return null;
            }
        }

        public virtual string GetNewPackPath(string path, string folderName, string guid = null) {
            if (folderName == null) throw new ArgumentNullException(nameof(folderName));
            if (path == null)
                return null;

            var fullPath = Path.Combine(path, folderName);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                return fullPath;

            if (guid != null) {
                if (TryHasGuid(guid, fullPath))
                    return path;
            }

            var i = 0;
            while (true) {
                i++;
                var pa = fullPath + i;
                if (!File.Exists(pa) && !Directory.Exists(pa))
                    return pa;
                if (guid == null)
                    continue;
                if (TryHasGuid(guid, pa))
                    return pa;
            }
        }

        public virtual IAbsoluteDirectoryPath GetRootedPath(string folder) {
            if (!(!string.IsNullOrWhiteSpace(folder))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(folder)");
            if (folder == ".")
                return Directory.GetCurrentDirectory().ToAbsoluteDirectoryPath();
            return Path.IsPathRooted(folder)
                ? folder.ToAbsoluteDirectoryPath()
                : Path.Combine(Directory.GetCurrentDirectory(), folder).ToAbsoluteDirectoryPath();
        }

        public virtual bool HasGuid(string guid, string path) {
            if (guid == null) throw new ArgumentNullException(nameof(guid));
            if (path == null) throw new ArgumentNullException(nameof(path));

            return GetGuid(path) == guid;
        }

        public virtual bool TryHasGuid(string guid, string path) {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return TryGetGuid(path) == guid;
        }
    }
}