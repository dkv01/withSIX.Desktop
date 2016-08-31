// <copyright company="SIX Networks GmbH" file="SimpleQueueExtension.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SN.withSIX.Core.Extensions
{
    public static class SimpleQueueExtension
    {
        public static Task SimpleQueue<T>(this IEnumerable<T> items, int maxThreads, Action<T> act) {
            var queue = new ConcurrentQueue<T>(items);

            var tasks = Enumerable.Range(1, maxThreads > 0
                ? Math.Min(maxThreads, queue.Count)
                : 1)
                .Select(x => TaskExtExt.StartLongRunningTask(() => {
                    T item;
                    while (queue.TryDequeue(out item))
                        act(item);
                }));

            return Task.WhenAll(tasks);
        }

        public static Task RunningQueue(this BlockingCollection<Func<Task>> block, int maxThreads,
            CancellationToken token = default(CancellationToken)) {
            var tasks = Enumerable.Range(1, maxThreads > 0
                ? maxThreads
                : 1)
                .Select(x => TaskExtExt.StartLongRunningTask(async () => {
                    foreach (var item in block.GetConsumingEnumerable(token))
                        await item().ConfigureAwait(false);
                }));

            return Task.WhenAll(tasks);
        }

        public static Task RunningQueue(this BlockingCollection<Action> block, int maxThreads,
            CancellationToken token = default(CancellationToken)) {
            var tasks = Enumerable.Range(1, maxThreads > 0
                ? maxThreads
                : 1)
                .Select(x => TaskExtExt.StartLongRunningTask(() => {
                    foreach (var item in block.GetConsumingEnumerable(token))
                        item();
                }));

            return Task.WhenAll(tasks);
        }


        public static Task RunningQueue<T>(this BlockingCollection<T> block, int maxThreads, Action<T> act) {
            var tasks = Enumerable.Range(1, maxThreads > 0
                ? maxThreads
                : 1)
                .Select(x => TaskExtExt.StartLongRunningTask(() => {
                    foreach (var item in block.GetConsumingEnumerable())
                        act(item);
                }));

            return Task.WhenAll(tasks);
        }

        public static async Task SimpleRunningQueue<T>(this IEnumerable<T> items, int maxThreads,
            Action<BlockingCollection<T>> mainAct, Action<T> act) {
            var queue = new ConcurrentQueue<T>(items);
            using (var blockingCollection = new BlockingCollection<T>(queue)) {
                var syncTask = TaskExtExt.StartLongRunningTask(() => mainAct(blockingCollection));
                var monitorTask = blockingCollection.RunningQueue(maxThreads, act);

                await syncTask.ConfigureAwait(false);
                blockingCollection.CompleteAdding();
                await monitorTask.ConfigureAwait(false);
            }
        }

        public static async Task SimpleRunningQueueAsync<T>(this IEnumerable<T> items, int maxThreads,
            Func<BlockingCollection<T>, Task> mainAct, Action<T> act) {
            var queue = new ConcurrentQueue<T>(items);
            using (var blockingCollection = new BlockingCollection<T>(queue)) {
                var syncTask = mainAct(blockingCollection);
                var monitorTask = blockingCollection.RunningQueue(maxThreads, act);

                await syncTask.ConfigureAwait(false);
                blockingCollection.CompleteAdding();
                await monitorTask.ConfigureAwait(false);
            }
        }

        public static Task StartConcurrentTaskQueue<T>(this IEnumerable<T> items, Func<T, Task> act, Func<int> count)
            => new ConcurrentTaskQueue<T>(items, act, count).Run();

        public static Task StartConcurrentTaskQueue<T>(this IEnumerable<T> items, CancellationToken token,
            Func<T, Task> act, Func<int> getMaxThreads, Func<int> count = null)
            => new ConcurrentTaskQueue<T>(items, act, getMaxThreads).Run(token);

        class ConcurrentTaskQueue<T>
        {
            readonly Func<T, Task> _act;
            readonly ConcurrentQueue<T> _cc;
            readonly Func<int> _getMaxCount;
            List<Task> _tasks;

            public ConcurrentTaskQueue(IEnumerable<T> items, Func<T, Task> act, Func<int> count = null) {
                _act = act;
                _getMaxCount = count ?? (() => 1);
                _cc = new ConcurrentQueue<T>(items);
                _tasks = new List<Task>();
            }

            public Task Run() => TaskExtExt.StartLongRunningTask(RunInternal);

            public Task Run(CancellationToken token)
                => Task.Factory.StartNew(() => RunInternal(token), token, TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();

            async Task RunInternal() {
                T item;
                while (_cc.TryDequeue(out item))
                    await ProcessIteration(item).ConfigureAwait(false);
                await Task.WhenAll(_tasks).ConfigureAwait(false);
            }

            async Task RunInternal(CancellationToken token) {
                T item;
                while (!token.IsCancellationRequested && _cc.TryDequeue(out item))
                    await ProcessIteration(item).ConfigureAwait(false);
                await Task.WhenAll(_tasks).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }

            Task ProcessIteration(T item) {
                _tasks.Add(RunTask(item));
                return ProcessQueue();
            }

            async Task ProcessQueue() {
                var maxCount = _getMaxCount();
                if (_tasks.Count >= maxCount) {
                    _tasks = _tasks.Where(x => !x.IsCompleted).ToList();
                    if (_tasks.Count >= maxCount)
                        await Task.WhenAny(_tasks).ConfigureAwait(false);
                }
            }

            // Wrapped in a Task.Run so that no processing in the action task can delay the queue
            Task RunTask(T item) => Task.Run(() => _act(item));
        }
    }
}