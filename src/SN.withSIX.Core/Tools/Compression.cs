// <copyright company="SIX Networks GmbH" file="Compression.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using NDepend.Path;
using SharpCompress.Archive;
using SharpCompress.Archive.GZip;
using SharpCompress.Archive.Zip;
using SharpCompress.Common;
using SharpCompress.Compressor.Deflate;
using SharpCompress.Writer;
using SharpCompress.Writer.Zip;
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
                ITProgress progress = null) {
                Contract.Requires<ArgumentNullException>(sourceFile != null);
                Contract.Requires<ArgumentNullException>(outputFolder != null);

                var ext = sourceFile.FileExtension;
                var options = fullPath ? ExtractOptions.ExtractFullPath : ExtractOptions.None;
                using (var archive = GetArchiveWithGzWorkaround(sourceFile, ext))
                    UnpackArchive(outputFolder, overwrite, archive, options, sourceFile);
            }

            public void PackTar(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile) {
                using (var tarStream = File.OpenWrite(outputFile.ToString()))
                using (var af = WriterFactory.Open(tarStream, ArchiveType.Tar, CompressionType.None)) {
                    foreach (var f in directory.DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                        af.Write(f.FullName.Replace(directory.ParentDirectoryPath + @"\", ""), f.FullName);
                    // This ommits the root folder ('userconfig')
                    //af.WriteAll(directory.ToString(), "*", SearchOption.AllDirectories);
                }
            }

            public void PackZip(IAbsoluteDirectoryPath directory, IAbsoluteFilePath outputFile) {
                using (var arc = ZipArchive.Create()) {
                    foreach (var f in directory.DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                        arc.AddEntry(f.FullName.Replace(directory.ParentDirectoryPath + @"\", ""), f.FullName);
                    arc.SaveTo(outputFile.ToString(), new CompressionInfo { DeflateCompressionLevel = CompressionLevel.BestCompression, Type = CompressionType.Deflate});
                }
            }

            static IArchive GetArchiveWithGzWorkaround(IAbsoluteFilePath sourceFile, string ext)
                => ext.Equals(".gz", StringComparison.OrdinalIgnoreCase)
                    ? GZipArchive.Open(sourceFile.ToString())
                    : ArchiveFactory.Open(sourceFile.ToString());

            static void UnpackArchive(IAbsoluteDirectoryPath outputFolder, bool overwrite, IArchive archive,
                ExtractOptions options, IAbsoluteFilePath sourceFile) {
                foreach (var p in archive.Entries.Where(entry => entry.IsDirectory)
                    .Select(entry => outputFolder.GetChildDirectoryWithName(entry.Key)))
                    p.MakeSurePathExists();

                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory)) {
                    if (overwrite)
                        options = options | ExtractOptions.Overwrite;

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

            public void UnpackSingle(string sourceFile, string destFile) {
                using (var archive = ArchiveFactory.Open(sourceFile)) {
                    var entry = archive.Entries.First();
                    entry.WriteToFile(destFile);
                }
            }

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
                        if (!Processes.Uac.CheckUac())
                            throw;
                        UnpackUpdater(sourceFile, outputFolder, overwrite, fullPath);
                    } catch (IOException) {
                        if (!Processes.Uac.CheckUac())
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