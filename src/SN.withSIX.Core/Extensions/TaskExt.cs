// <copyright company="SIX Networks GmbH" file="TaskExt.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ShortBus;

namespace SN.withSIX.Core.Extensions
{
    // TODO: re-eval https://gist.github.com/rizal-almashoor/2818038
    public static class TaskExt
    {
        public static readonly Task Default = Empty<int>.Task;
        public static readonly Action NullAction = () => { };

        public static async Task<UnitType> Void<T>(this Task<T> task) {
            await task.ConfigureAwait(false);
            return UnitType.Default;
        }

        public static async Task<UnitType> Void(this Task task) {
            await task.ConfigureAwait(false);
            return UnitType.Default;
        }

        [Obsolete("Use actual async/await!")]
        public static void WaitAndUnwrapException(this Task task) {
            try {
                task.Wait();
            } catch (AggregateException ex) {
                throw ex.ThrowFirstInner();
            }
        }

        [Obsolete("Use actual async/await!")]
        public static void WaitAllAndUnwrapException(this Task[] tasks) {
            try {
                Task.WaitAll(tasks);
            } catch (AggregateException ex) {
                throw ex.ThrowFirstInner();
            }
        }

        [Obsolete("Use actual async/await!")]
        public static void WaitAndUnwrapException(this Task task, CancellationToken cancellationToken) {
            try {
                task.Wait(cancellationToken);
            } catch (AggregateException ex) {
                throw ex.ThrowFirstInner();
            }
        }

        [Obsolete("Use actual async/await!")]
        public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task) {
            try {
                return task.Result;
            } catch (AggregateException ex) {
                throw ex.ThrowFirstInner();
            }
        }

        [Obsolete("Use actual async/await!")]
        public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task,
            CancellationToken cancellationToken) {
            try {
                task.Wait(cancellationToken);
                return task.Result;
            } catch (AggregateException ex) {
                throw ex.ThrowFirstInner();
            }
        }

        public static async Task<TDesired> To<TFrom, TDesired>(this Task<TFrom> task) where TFrom : TDesired
            => (TDesired) await task.ConfigureAwait(false);

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int delay, CancellationTokenSource cancel) {
            try {
                return await TimeoutAfter(task, delay, cancel.Token).ConfigureAwait(false);
            } finally {
                cancel.Cancel();
            }
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int delay, CancellationToken token) {
            if (task == null)
                throw new NullReferenceException();

            if (task.IsCompleted || (delay == Timeout.Infinite))
                return await task;

            await Task.WhenAny(task, Task.Delay(delay, token)).ConfigureAwait(false);

            if (!task.IsCompleted)
                throw new TimeoutException("Timeout hit.");

            return await task.ConfigureAwait(false);
        }

        public static async Task TimeoutAfter(this Task task, int delay, CancellationTokenSource cancel) {
            try {
                await TimeoutAfter(task, delay, cancel.Token).ConfigureAwait(false);
            } finally {
                cancel.Cancel();
            }
        }

        public static async Task TimeoutAfter(this Task task, int delay, CancellationToken token) {
            if (task == null)
                throw new NullReferenceException();

            if (task.IsCompleted || (delay == Timeout.Infinite)) {
                await task.ConfigureAwait(false);
                return;
            }

            await Task.WhenAny(task, Task.Delay(delay, token)).ConfigureAwait(false);

            if (!task.IsCompleted)
                throw new TimeoutException("Timeout hit.");
        }

        public static class Empty<T>
        {
            public static Task<T> Task { get; } = System.Threading.Tasks.Task.FromResult(default(T));
        }
    }

    public static class ExceptionHelpers
    {
        public static Exception ThrowFirstInner(this AggregateException ex) => GetFirstException(ex).ReThrow();

        public static Exception GetFirstException(this AggregateException ex) => ex.Flatten().InnerException;

        public static Exception ReThrowInner(this Exception ex) => ex.InnerException.ReThrow();

        static Exception ReThrow(this Exception exception) {
            ExceptionDispatchInfo.Capture(exception).Throw();
            return exception;
        }
    }
}