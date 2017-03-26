// <copyright company="SIX Networks GmbH" file="ProcessStartInfoBuilder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core.Extensions;

namespace withSIX.Core.Services.Infrastructure
{
    public class ProcessStartInfoBuilder
    {
        public ProcessStartInfoBuilder() {}

        public ProcessStartInfoBuilder(string exePath) : this() {
            ExePath = exePath;
        }

        public ProcessStartInfoBuilder(string exePath, string arguments) : this(exePath) {
            Arguments = arguments;
        }

        public ProcessStartInfoBuilder(IAbsoluteFilePath exePath)
            : this() {
            ExePath = exePath.ToString();
        }

        public ProcessStartInfoBuilder(IAbsoluteFilePath exePath, string arguments)
            : this(exePath) {
            Arguments = arguments;
        }

        public IAbsoluteDirectoryPath WorkingDirectory { get; set; }
        public bool Redirect { get; set; }
        public string ExePath { get; set; }
        public string Arguments { get; set; }

        //public ProcessWindowStyle WindowStyle { get; set; }

        public ProcessStartInfo Build() {
            var info = new ProcessStartInfo(ExePath, Arguments)
                .SetWorkingDirectoryOrDefault(WorkingDirectory);

            //info.WindowStyle = WindowStyle;

            if (Redirect)
                info = info.EnableRedirect();

            return info;
        }
    }

    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process,
            CancellationToken cancellationToken = default(CancellationToken)) {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }

        public static void ConfirmSuccess(this ProcessExitResult result) {
            if (result.ExitCode != 0)
                throw result.GenerateException();
        }

        public static ProcessException GenerateException(this ProcessExitResult exitResult)
            => new ProcessException(GenerateMessage(exitResult));

        static string GenerateMessage(ProcessExitResult exitResult)
            => "The process with ID: " + exitResult.Id + ", did not exit gracefully, CODE: " + exitResult.ExitCode;

        public static ProcessException GenerateException(this ProcessExitResultWithOutput exitResult) {
            var output = exitResult.StandardOutput +
                         "\nError: " + exitResult.StandardError;
            return new ProcessException(GenerateMessage(exitResult) + "\nOutput: " + output) {Output = output};
        }
    }

    public class ProcessException : Exception
    {
        public ProcessException(string message) : base(message) {}

        public string Output { get; set; }
    }
}