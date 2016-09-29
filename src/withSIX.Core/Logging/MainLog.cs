// <copyright company="SIX Networks GmbH" file="MainLog.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace withSIX.Core.Logging
{
    public class MainLog
    {
        public static Lazy<ILogManager> logManager { get; set; }
        static readonly Lazy<ILogger> logger = new Lazy<ILogger>(() => LogManager.GetCurrentClassLoggerOrMerged());
        static readonly DateTime startTime = Process.GetCurrentProcess().StartTime;
        public static ILogManager LogManager => logManager.Value;
        public static ILogger Logger => logger.Value;

        public static Bencher Bench(string message = null, [CallerMemberName] string caller = null) =>
            Common.Flags.Verbose
                ? new Bencher(message, caller)
                : null;

        public static void BenchLog(string message) {
            var time = Tools.Generic.GetCurrentUtcDateTime - startTime;
            Logger.Debug("::BENCH:: (" + time.ToString("mm':'ss':'fff") + ") " + message);
        }
    }
}