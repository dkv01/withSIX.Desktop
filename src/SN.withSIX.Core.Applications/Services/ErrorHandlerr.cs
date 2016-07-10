using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using ReactiveUI;
using SharpCompress.Archive;
using SharpCompress.Archive.Zip;
using SharpCompress.Common;
using SharpCompress.Compressor.Deflate;
using SN.withSIX.Core.Applications.Errors;

namespace SN.withSIX.Core.Applications.Services
{
    public static class ErrorHandlerr
    {
        private static IExceptionHandler _exceptionHandler;


        public static Task GenerateDiagnosticZip(IAbsoluteFilePath path) {
            var d = GetLogFilesDictionary(Common.Paths.LocalDataRootPath);
            return
                Task.Run(
                    () => {
                        using (var arc = ZipArchive.Create()) {
                            foreach (var f in d)
                                arc.AddEntry(f.Key, f.Value);
                            arc.SaveTo(path.ToString(),
                                new CompressionInfo {
                                    DeflateCompressionLevel = CompressionLevel.BestCompression,
                                    Type = CompressionType.Deflate
                                });
                        }
                    });
        }


        static Dictionary<string, string> GetLogFilesDictionary(IAbsoluteDirectoryPath path)
            => path.DirectoryInfo.GetFiles("*.log", SearchOption.AllDirectories)
                .ToDictionary(x => x.FullName.Replace(path + "\\", ""), x => x.FullName);

        public static Task<bool> TryAction(Func<Task> act, string action = null)
            => _exceptionHandler.TryExecuteAction(act, action);

        public static void SetExceptionHandler(IExceptionHandler handler) {
            _exceptionHandler = handler;
        }

        public static UserError HandleException(Exception ex, string action = "Action")
            => ErrorHandlerr._exceptionHandler.HandleException(ex, action);


        public static void RegisterHandler(IExceptionHandlerHandle exceptionHandlerHandle) {
            ErrorHandlerr._exceptionHandler.RegisterHandler(exceptionHandlerHandle);
        }
    }
}