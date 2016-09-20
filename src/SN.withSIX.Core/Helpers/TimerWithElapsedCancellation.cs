// <copyright company="SIX Networks GmbH" file="TimerWithElapsedCancellation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core.Helpers
{
    public class TimerWithElapsedCancellation : TimerWithElapsedCancellationAsync
    {
        public TimerWithElapsedCancellation(double time, Func<bool> onElapsed, Action onDisposed = null)
            : base(time, async () => onElapsed(), onDisposed) {}

        public TimerWithElapsedCancellation(TimeSpan time, Func<bool> onElapsed, Action onDisposed = null)
            : this(time.TotalMilliseconds, onElapsed, onDisposed) {}
    }

    public abstract class Timer : IDisposable
    {
        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool v) {}
    }

    public class ElapsedEventArgs {}

    public class TimerWithElapsedCancellationOnExceptionOnly : TimerWithElapsedCancellationAsync
    {
        public TimerWithElapsedCancellationOnExceptionOnly(double time, Action onElapsed)
            : base(time, async () => onElapsed()) {}

        public TimerWithElapsedCancellationOnExceptionOnly(TimeSpan time, Action onElapsed)
            : this(time.TotalMilliseconds, onElapsed) {}
    }

    public class TimerWithoutOverlap : TimerWithElapsedCancellationAsync
    {
        public TimerWithoutOverlap(double time, Action onElapsed)
            : base(time, async () => onElapsed()) {}

        public TimerWithoutOverlap(TimeSpan time, Action onElapsed)
            : base(time, async () => onElapsed()) {}
    }

    public class TimerWithElapsedCancellationAsync : Timer
    {
        private readonly Action _onDisposed;
        private CancellationTokenSource _cts;

        private bool _disposed;
        private Task _task;
        protected Func<Task<bool>> OnElapsedFunc;

        public TimerWithElapsedCancellationAsync(double time, Func<Task<bool>> onElapsed, Action onDisposed = null) {
            _onDisposed = onDisposed;
            _cts = new CancellationTokenSource();
            _task = Task.Run(async () => {
                while (!_cts.IsCancellationRequested) {
                    await Task.Delay(TimeSpan.FromMilliseconds(time), _cts.Token).ConfigureAwait(false);
                    try {
                        if (!await onElapsed().ConfigureAwait(false)) {
                            break;
                        }
                    } catch (Exception ex) {
                        MainLog.Logger.Warn("Unhandled Ex in timer", ex);
                        break;
                    }
                }
            }, _cts.Token);
        }

        public TimerWithElapsedCancellationAsync(TimeSpan time, Func<Task<bool>> onElapsed, Action onDisposed = null)
            : this(time.TotalMilliseconds, onElapsed, onDisposed) {}

        public TimerWithElapsedCancellationAsync(double time, Func<Task> onElapsedNonBool, Action onDisposed = null)
            : this(time, () => Wrap(onElapsedNonBool), onDisposed) {}

        public TimerWithElapsedCancellationAsync(TimeSpan time, Func<Task> onElapsedNonBool, Action onDisposed = null)
            : this(time.TotalMilliseconds, onElapsedNonBool, onDisposed) {}

        protected override void Dispose(bool disposing) {
            if (_disposed)
                return;
            _disposed = true;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
            _task = null;
            _onDisposed?.Invoke();
        }

        static async Task<bool> Wrap(Func<Task> task) {
            await task().ConfigureAwait(false);
            return true;
        }
    }
}