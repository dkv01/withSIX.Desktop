using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Services.Infrastructure;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications.Services
{
    public class SteamHelperRunner {

        public async Task RunHelperInternal(CancellationToken cancelToken, IEnumerable<string> parameters, Action<Process, string> standardOutputAction, Action<Process, string> standardErrorAction) {
            var helperExe = GetHelperExecutable();
            var r =
                await
                    Tools.ProcessManager.LaunchAndProcessAsync(
                        new LaunchAndProcessInfo(new ProcessStartInfo(helperExe.ToString(),
                            parameters.CombineParameters()) {
                            WorkingDirectory = helperExe.ParentDirectoryPath.ToString()
                        }) {
                            StandardOutputAction = standardOutputAction,
                            StandardErrorAction = standardErrorAction,
                            CancellationToken = cancelToken
                        }).ConfigureAwait(false);
            ProcessExitResult(r);
        }

        private static void ProcessExitResult(ProcessExitResult r) {
            switch (r.ExitCode) {
            case 3:
                // TODO
                throw new SteamInitializationException(
                    "The Steam client does not appear to be running, or runs under different (Administrator?) priviledges. Please start Steam and/or restart the withSIX client under the same priviledges");
            case 4:
                throw new SteamNotFoundException(
                    "The Steam client does not appear to be running, nor was Steam found");
            case 9:
                throw new TimeoutException("The operation timed out waiting for a response from the Steam client");
            case 10:
                throw new OperationCanceledException("The operation was canceled");
            }
            r.ConfirmSuccess();
        }

        private static IAbsoluteFilePath GetHelperExecutable() => Common.Paths.AppPath
            .GetChildFileWithName("withSIX.SteamHelper.exe");
    }
}