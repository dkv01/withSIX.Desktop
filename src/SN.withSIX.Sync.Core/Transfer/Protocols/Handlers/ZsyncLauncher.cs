// <copyright company="SIX Networks GmbH" file="ZsyncLauncher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services;
using SN.withSIX.Core.Services.Infrastructure;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Sync.Core.Transfer.Protocols.Handlers
{
    public class ZsyncParams
    {
        public ZsyncParams(ITransferProgress progress, Uri uri, IAbsoluteFilePath file) {
            Progress = progress;
            Uri = uri;
            File = file;
        }

        public ITransferProgress Progress { get; }
        public Uri Uri { get; }
        public IAbsoluteFilePath File { get; }
        public CancellationToken CancelToken { get; set; }
        public IAbsoluteFilePath ExistingFile { get; set; }
    }

    public interface IZsyncLauncher
    {
        //ProcessExitResultWithOutput Run(Uri uri, string file);
        // TODO: Param objects
        ProcessExitResultWithOutput RunAndProcess(ZsyncParams p);

        Task<ProcessExitResultWithOutput> RunAndProcessAsync(ZsyncParams pa);
    }

    public class ZsyncLauncher : IZsyncLauncher
    {
        const bool UseCygwinZsync = true;
        static readonly string[] tempExtensions = {".zs-old", Tools.GenericTools.TmpExtension, ".part"};
        readonly IAuthProvider _authProvider;
        readonly IAbsoluteFilePath _binPath;
        readonly ZsyncOutputParser _parser;
        readonly IProcessManager _processManager;

        public ZsyncLauncher(IProcessManager processManager, IPathConfiguration configuration,
            ZsyncOutputParser parser, IAuthProvider authProvider) {
            if (processManager == null)
                throw new ArgumentNullException(nameof(processManager));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _authProvider = authProvider;

            _processManager = processManager;
            _parser = parser;
            _binPath = configuration.ToolCygwinBinPath.GetChildFileWithName("zsync.exe");
        }

        public ProcessExitResultWithOutput RunAndProcess(ZsyncParams p) {
            TryHandleOldFiles(p.File);
            var processInfo = BuildProcessInfo(p);
            var r =
                ProcessExitResultWithOutput.FromProcessExitResult(_processManager.LaunchAndProcess(processInfo),
                    p.Progress.Output);
            if (r.ExitCode == 0)
                TryRemoveOldFiles(p.File);
            return r;
        }

        public async Task<ProcessExitResultWithOutput> RunAndProcessAsync(ZsyncParams p) {
            TryHandleOldFiles(p.File);
            var processInfo = BuildProcessInfo(p);
            var r =
                ProcessExitResultWithOutput.FromProcessExitResult(
                    await _processManager.LaunchAndProcessAsync(processInfo).ConfigureAwait(false),
                    p.Progress.Output);
            if (r.ExitCode == 0)
                TryRemoveOldFiles(p.File);
            return r;
        }

        public ProcessExitResultWithOutput Run(ZsyncParams p) {
            TryHandleOldFiles(p.File);
            var startInfo = new ProcessStartInfo(_binPath.ToString(), GetArgs(p))
                .SetWorkingDirectoryOrDefault(p.File.ParentDirectoryPath);
            var r = _processManager.LaunchAndGrab(new BasicLaunchInfo(startInfo));
            if (r.ExitCode == 0)
                TryRemoveOldFiles(p.File);
            return r;
        }

        LaunchAndProcessInfo BuildProcessInfo(ZsyncParams p) => new LaunchAndProcessInfo(GetProcessStartInfo(p)) {
            StandardOutputAction = (x, args) => ParseOutput(x, args, p.Progress),
            StandardErrorAction = (x, args) => ParseOutput(x, args, p.Progress),
            MonitorOutput = _processManager.DefaultMonitorOutputTimeOut,
            MonitorResponding = _processManager.DefaultMonitorRespondingTimeOut,
            CancellationToken = p.CancelToken
        };

        static void TryHandleOldFiles(IAbsoluteFilePath localFile) {
            try {
                RemoveReadOnlyFromOldFiles(localFile);
            } catch (Exception e) {
                MainLog.Logger.FormattedDebugException(e);
            }
        }

        static void TryRemoveOldFiles(IAbsoluteFilePath localFile) {
            try {
                RemoveOldFiles(localFile);
            } catch (Exception e) {
                MainLog.Logger.FormattedDebugException(e);
            }
        }

        static void RemoveReadOnlyFromOldFiles(IAbsoluteFilePath localFile) {
            foreach (var file in tempExtensions.Select(x => localFile + x))
                file.RemoveReadonlyWhenExists();
        }

        static void RemoveOldFiles(IAbsoluteFilePath localFile) {
            foreach (var file in tempExtensions.Select(x => localFile + x))
                Tools.FileUtil.Ops.DeleteIfExists(file);
            // TODO: Perhaps move more to an overall cleaner..
            var sf = localFile.ParentDirectoryPath.GetChildFileWithName("zsync.exe.stackdump");
            if (sf.Exists)
                sf.Delete();
        }

        void ParseOutput(Process sender, string data, ITransferProgress progress) {
            _parser.ParseOutput(sender, data, progress);
        }

        ProcessStartInfo GetProcessStartInfo(ZsyncParams p) => new ProcessStartInfoBuilder(_binPath, GetArgs(p)) {
            WorkingDirectory = p.File.ParentDirectoryPath
        }.Build();

        string GetArgs(ZsyncParams p) => !string.IsNullOrWhiteSpace(p.Uri.UserInfo)
            ? GetArgsWithAuthInfo(p)
            : GetArgsWithoutAuthInfo(p);

        static string GetArgsWithoutAuthInfo(ZsyncParams p) =>
            $"{GetInputFile(p)}{GetDebugInfo()}-o \"{HandlePath(p.File)}\" \"{p.Uri.EscapedUri()}\"";

        string GetArgsWithAuthInfo(ZsyncParams p) =>
            $"{GetInputFile(p)}{GetAuthInfo(p.Uri)}{GetDebugInfo()}-o \"{HandlePath(p.File)}\" \"{GetUri(p.Uri).EscapedUri()}\"";

        string GetAuthInfo(Uri uri) {
            var hostname = uri.Host;
            if (uri.Port != 80 && uri.Port != 443)
                hostname += ":" + uri.Port;

            var authInfo = _authProvider.GetAuthInfoFromUri(uri);
            return $"-A {hostname}={authInfo.Username}:{authInfo.Password} ";
        }

        static string GetDebugInfo() => Common.Flags.Verbose ? "-v " : null;

        Uri GetUri(Uri uri) => !string.IsNullOrWhiteSpace(uri.UserInfo) ? _authProvider.HandleUriAuth(uri) : uri;

        static string GetInputFile(ZsyncParams p)
            => p.ExistingFile != null && p.ExistingFile.Exists ? $"-i \"{HandlePath(p.ExistingFile)}\" " : string.Empty;
#pragma warning disable 162
        static string HandlePath(IAbsoluteFilePath path) => UseCygwinZsync
            ? path.ToString().CygwinPath()
            : path.ToString().MingwPath();
    }
}