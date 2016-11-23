// <copyright company="SIX Networks GmbH" file="Bencher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace withSIX.Core.Logging
{
    public static class BenchExtension
    {
        public static Bencher Bench(this object obj, string message = null, string startMessage = null,
                [CallerMemberName] string caller = null)
            => Common.Flags.Verbose ? new Bencher(message, obj.GetType().Name + "." + caller, startMessage) : null;
    }

    public class Bencher : IDisposable
    {
        readonly string _caller;
        readonly string _message;
        readonly Stopwatch _stopWatch;

        public Bencher(string message, string caller, string startInfo = null) {
            _message = message;
            _caller = caller;
            _stopWatch = Stopwatch.StartNew();
            MainLog.BenchLog($"START: [{_caller}] {_message}");
            if (startInfo != null)
                MainLog.BenchLog(startInfo);
        }

        public object AdditionalInfo { get; set; }

        public void Dispose() {
            _stopWatch.Stop();
            MainLog.BenchLog(
                $"END: {_stopWatch.Elapsed.ToString("mm':'ss':'fff")} [{_caller}] {_message} ({AdditionalInfo})");
        }
    }
}