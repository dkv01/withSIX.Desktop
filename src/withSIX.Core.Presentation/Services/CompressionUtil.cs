// <copyright company="SIX Networks GmbH" file="CompressionUtil.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using NDepend.Path;
using SharpCompress.Archives;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;

namespace withSIX.Core.Presentation.Services
{
    public class CompressionUtil : ICompressionUtil, IPresentationService
    {
        public void UnpackInternal(IAbsoluteFilePath sourceFile, IAbsoluteDirectoryPath outputFolder,
            bool overwrite = false, bool fullPath = true, bool checkFileIntegrity = true,
            ITProgress progress = null) {
            if (sourceFile == null) throw new ArgumentNullException(nameof(sourceFile));
            if (outputFolder == null) throw new ArgumentNullException(nameof(outputFolder));

            var ext = sourceFile.FileExtension;
            var options = new ExtractionOptions {PreserveFileTime = true};
            if (fullPath)
                options.ExtractFullPath = true;
            if (overwrite)
                options.Overwrite = true;
            using (var archive = GetArchiveWithGzWorkaround(sourceFile, ext))
                UnpackArchive(outputFolder, overwrite, archive, options, sourceFile);
        }

        public void CreateTar(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile) {
            using (var tarStream = File.OpenWrite(outputFile.ToString()))
            using (var af = WriterFactory.Open(tarStream, ArchiveType.Tar, CompressionType.None)) {
                foreach (var f in directory.DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    af.Write(f.FullName.Replace(directory.ParentDirectoryPath + @"\", ""), f.FullName);
                // This ommits the root folder ('userconfig')
                //af.WriteAll(directory.ToString(), "*", SearchOption.AllDirectories);
            }
        }

        public void CreateZip(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile) {
            using (var arc = ZipArchive.Create()) {
                foreach (var f in directory.DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    arc.AddEntry(f.FullName.Replace(directory.ParentDirectoryPath + @"\", ""), f.FullName);
                arc.SaveTo(outputFile.ToString(),
                    new ZipWriterOptions(CompressionType.Deflate) {
                        DeflateCompressionLevel = CompressionLevel.BestCompression
                    });
            }
        }

        public void UnpackSingleInternal(string sourceFile, string destFile) {
            using (var archive = ArchiveFactory.Open(sourceFile)) {
                var entry = archive.Entries.First();
                entry.WriteToFile(destFile);
            }
        }

        public void UnpackGzip(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destFile, ITProgress progress = null) {
            using (var archive = GZipArchive.Open(sourceFile.ToString())) {
                try {
                    TryUnpackArchive(destFile, progress, archive);
                } catch (ZlibException ex) {
                    if ((ex.Message == "Not a valid GZIP stream.") || (ex.Message == "Bad GZIP header.")
                        || (ex.Message == "Unexpected end-of-file reading GZIP header.")
                        || (ex.Message == "Unexpected EOF reading GZIP header.")) {
                        var header = TryReadHeader(sourceFile);
                        throw new CompressedFileException(
                            $"The archive appears corrupt: {sourceFile}. Header:\n{header}", ex);
                    }
                    throw;
                }
            }
        }

        public void PackFiles(IEnumerable<KeyValuePair<string, string>> items, IAbsoluteFilePath path) {
            using (var arc = ZipArchive.Create()) {
                foreach (var f in items)
                    arc.AddEntry(f.Key, f.Value);
                arc.SaveTo(path.ToString(),
                    new ZipWriterOptions(CompressionType.Deflate) {
                        DeflateCompressionLevel = CompressionLevel.BestCompression
                    });
            }
        }

        private IArchive GetArchiveWithGzWorkaround(IAbsoluteFilePath sourceFile, string ext)
            => ext.Equals(".gz", StringComparison.OrdinalIgnoreCase)
                ? GZipArchive.Open(sourceFile.ToString())
                : ArchiveFactory.Open(sourceFile.ToString());

        private void UnpackArchive(IAbsoluteDirectoryPath outputFolder, bool overwrite, IArchive archive,
            ExtractionOptions options, IAbsoluteFilePath sourceFile) {
            foreach (var p in archive.Entries.Where(entry => entry.IsDirectory)
                .Select(entry => outputFolder.GetChildDirectoryWithName(entry.Key)))
                p.MakeSurePathExists();

            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory)) {
                var fileName = entry.Key ?? sourceFile.FileNameWithoutExtension;
                var destinationFile = outputFolder.GetChildFileWithName(fileName);
                if (overwrite) {
                    destinationFile.MakeSureParentPathExists();
                    entry.WriteToFile(destinationFile.ToString(), options);
                } else {
                    if (destinationFile.Exists)
                        continue;
                    destinationFile.MakeSureParentPathExists();
                    entry.WriteToFile(destinationFile.ToString(), options);
                }
            }
        }

        private static string TryReadHeader(IAbsoluteFilePath sourceFile) {
            try {
                return ReadHeader(sourceFile);
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Could not read header from corrupt gzip file");
                return "$W6$: Could not read file";
            }
        }

        private static string ReadHeader(IAbsoluteFilePath sourceFile) {
            if (!sourceFile.Exists)
                return "$W6$: file does not exist";
            var buffer = new byte[1024];
            using (var f = new FileStream(sourceFile.ToString(), FileMode.Open))
                f.Read(buffer, 0, (int) Math.Min(f.Length, 1024));
            return GetString(buffer);
        }

        private static string GetString(byte[] bytes) {
            var chars = new char[bytes.Length/sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private static void TryUnpackArchive(IAbsoluteFilePath destFile, ITProgress progress, GZipArchive archive) {
            destFile.RemoveReadonlyWhenExists();
            var entry = archive.Entries.First();
            if (progress != null) {
                var startTime = DateTime.UtcNow;
                archive.CompressedBytesRead += (sender, args) => {
                    double prog = args.CompressedBytesRead/(float) archive.TotalSize;
                    if (prog > 1)
                        prog = 1;
                    var totalMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    progress.Update(
                        totalMilliseconds > 0 ? (long?) (args.CompressedBytesRead/(totalMilliseconds/1000.0)) : null,
                        prog*100);
                };
            }
            entry.WriteToFile(destFile.ToString());
            progress?.Update(null, 100);
        }
    }
}