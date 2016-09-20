// <copyright company="SIX Networks GmbH" file="GzipTools.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using NDepend.Path;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Services.Infrastructure;
using GZipStream = System.IO.Compression.GZipStream;

namespace SN.withSIX.Core
{
    public partial class Tools
    {
        public class GzipTools
        {
            private const double DefaultPredictedCompressionRatio = 0.85;

            public virtual string Gzip(IAbsoluteFilePath file, IAbsoluteFilePath dest = null,
                bool preserveFileNameAndModificationTime = true, ITProgress status = null) {
                Contract.Requires<ArgumentNullException>(file != null);
                Contract.Requires<ArgumentException>(file.Exists);

                var defDest = (file + ".gz").ToAbsoluteFilePath();
                if (dest == null)
                    dest = defDest;

                var cmd = $"-f --best --rsyncable --keep \"{file}\"";
                if (!preserveFileNameAndModificationTime)
                    cmd = "-n " + cmd;

                dest.RemoveReadonlyWhenExists();

                var startInfo =
                    new ProcessStartInfoBuilder(Common.Paths.ToolPath.GetChildFileWithName("gzip.exe"), cmd) {
                        WorkingDirectory = file.ParentDirectoryPath
                    }.Build();

                var srcSize = file.FileInfo.Length;
                ProcessExitResultWithOutput ret;
                var predictedSize = srcSize*DefaultPredictedCompressionRatio;
                using (StatusProcessor.Conditional(defDest, status, (long) predictedSize))
                    ret = ProcessManager.LaunchAndGrabTool(startInfo, "Gzip pack");
                if (Path.GetFullPath(dest.ToString()) != Path.GetFullPath(defDest.ToString()))
                    FileUtil.Ops.MoveWithRetry(defDest, dest);

                return ret.StandardOutput + ret.StandardError;
            }

            public virtual string GzipStdOut(IAbsoluteFilePath inputFile, IAbsoluteFilePath outputFile = null,
                bool preserveFileNameAndModificationTime = true, ITProgress status = null) {
                Contract.Requires<ArgumentException>(inputFile != null);
                Contract.Requires<ArgumentException>(inputFile.Exists);

                if (outputFile == null)
                    outputFile = (inputFile + ".gz").ToAbsoluteFilePath();

                var cmd = $"-f --best --rsyncable --keep --stdout \"{inputFile}\" > \"{outputFile}\"";
                if (!preserveFileNameAndModificationTime)
                    cmd = "-n " + cmd;

                outputFile.RemoveReadonlyWhenExists();
                var startInfo =
                    new ProcessStartInfoBuilder(Common.Paths.ToolPath.GetChildFileWithName("gzip.exe"), cmd) {
                        WorkingDirectory = Common.Paths.LocalDataPath
                    }.Build();
                var srcSize = inputFile.FileInfo.Length;
                ProcessExitResultWithOutput ret;
                var predictedSize = srcSize*DefaultPredictedCompressionRatio;
                using (StatusProcessor.Conditional(outputFile, status, (long) predictedSize))
                    ret = ProcessManager.LaunchAndGrabToolCmd(startInfo, "Gzip pack");
                return ret.StandardOutput + ret.StandardError;
            }

            public virtual string GzipAuto(IAbsoluteFilePath inputFile, IAbsoluteFilePath outputFile = null,
                bool preserveFileNameAndModificationTime = true, ITProgress status = null) {
                if (inputFile.ToString().EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    return GzipStdOut(inputFile, outputFile, preserveFileNameAndModificationTime, status);
                return Gzip(inputFile, outputFile, preserveFileNameAndModificationTime, status);
            }

            public virtual byte[] DecompressGzip(byte[] compressedBytes) {
                Contract.Requires<ArgumentNullException>(compressedBytes != null);

                using (var ms = new MemoryStream(compressedBytes)) {
                    var step = new byte[16]; //Instead of 16 can put any 2^x
                    using (var zip = new GZipStream(ms, CompressionMode.Decompress))
                    using (var outStream = new MemoryStream()) {
                        int readCount;
                        do {
                            readCount = zip.Read(step, 0, step.Length);
                            outStream.Write(step, 0, readCount);
                        } while (readCount > 0);
                        return outStream.ToArray();
                    }
                }
            }

            public void UnpackSingleGzip(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destFile,
                ITProgress progress = null) {
                _compressionUtil.UnpackGzip(sourceFile, destFile, progress);
            }

            public void UnpackSingleGzipWithFallbackAndRetry(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destFile) {
                FileUtil.Ops.AddIORetryDialog(() => {
                    try {
                        UnpackSingleGzip(sourceFile, destFile);
                    } catch (UnauthorizedAccessException) {
                        if (!UacHelper.CheckUac())
                            throw;
                        UnpackSingleZipWithUpdaters(sourceFile, destFile);
                    }
                });
            }

            static void UnpackSingleZipWithUpdaters(IAbsoluteFilePath sourceFile, IAbsoluteFilePath destFile) {
                Generic.RunUpdater(UpdaterCommands.UnpackSingleGzip, sourceFile.ToString(), destFile.ToString());
            }
        }

        internal class StatusProcessor : IDisposable
        {
            private readonly IAbsoluteFilePath _dest;
            private readonly long _predictedSize;
            private readonly DateTime _startTime;
            private readonly ITProgress _status;
            private readonly TimerWithElapsedCancellationOnExceptionOnly _timer;

            internal StatusProcessor(IAbsoluteFilePath dest, ITProgress status, long predictedSize) {
                _dest = dest;
                _status = status;
                _predictedSize = predictedSize;
                _startTime = DateTime.UtcNow;
                _timer = new TimerWithElapsedCancellationOnExceptionOnly(TimeSpan.FromMilliseconds(250),
                    CalculateProgressAndSpeed);
            }

            public void Dispose() => _timer.Dispose();

            public static IDisposable Conditional(IAbsoluteFilePath dest, ITProgress status, long predictedSize)
                => status == null || predictedSize == 0 ? null : new StatusProcessor(dest, status, predictedSize);

            private void CalculateProgressAndSpeed() {
                if (!_dest.Exists)
                    return;
                var destSize = _dest.FileInfo.Length;
                var totalMilliseconds = (DateTime.UtcNow - _startTime).TotalMilliseconds;
                _status.Update(totalMilliseconds > 0 ? (long?) (destSize/(totalMilliseconds/1000.0)) : null,
                    Math.Min(destSize, _predictedSize)/_predictedSize*100);
            }
        }
    }


    public class CompressedFileException : Exception
    {
        public CompressedFileException(string message) : base(message) {}
        public CompressedFileException(string message, Exception innerException) : base(message, innerException) {}
    }
}