// <copyright company="SIX Networks GmbH" file="TdfExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SN.withSIX.Core.Applications.Extensions
{
    public static class TdfExtensions
    {
        public static IObservable<Unit> ConcatTaskTdf<T>(this IObservable<T> obs, Func<Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, 1);

        public static IObservable<T2> ConcatTaskTdf<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory)
            => obs.MergeTaskTdf(taskFactory, 1);

        public static IObservable<Unit> ConcatTaskTdf<T>(this IObservable<T> obs, Func<T, Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, 1);

        public static IObservable<T2> ConcatTaskTdf<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory)
            => obs.MergeTaskTdf(taskFactory, 1);


        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);

        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);

        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<T, Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);

        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);

        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<Task> taskFactory,
            int degreeOfParallism)
            => obs.FromTdf(taskFactory.ToTransformBlock<T>(BuildMaxDegreeOption(degreeOfParallism)));

        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory,
            int degreeOfParallism)
            => obs.FromTdf(taskFactory.ToTransformBlock<T, T2>(BuildMaxDegreeOption(degreeOfParallism)));

        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<T, Task> taskFactory,
            int degreeOfParallism) => obs.FromTdf(taskFactory.ToTransformBlock(BuildMaxDegreeOption(degreeOfParallism)));

        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory,
            int degreeOfParallism)
            => obs.FromTdf(taskFactory.ToTransformBlock(BuildMaxDegreeOption(degreeOfParallism)));

        private static ExecutionDataflowBlockOptions BuildMaxDegreeOption(int degreeOfParallism)
            => new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = degreeOfParallism
            };

        // Original example was with Observable.Defer, however it didn't dispose from the source subscription!
        public static IObservable<TResult> FromTdf<T, TResult>(this IObservable<T> source,
            Func<IPropagatorBlock<T, TResult>> blockFactory) => Observable.Create<TResult>(observer => {
                var block = blockFactory();
                var dsp1 = block.AsObservable().Subscribe(observer.OnNext);
                var dsp2 = source.Subscribe(block.AsObserver());
                return new CompositeDisposable {dsp2, dsp1};
            });

        public static IObservable<TResult> FromTdf<T, TResult>(this IObservable<T> source,
            IPropagatorBlock<T, TResult> block) => source.FromTdf(() => block);

        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<Task> taskFactory)
            => new TransformBlock<T, Unit>(_ => taskFactory.VoidReactive());

        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<Task> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, Unit>(_ => taskFactory.VoidReactive(), options);

        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<T, Task> taskFactory)
            => new TransformBlock<T, Unit>(taskFactory.VoidReactive());

        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<T, Task> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, Unit>(taskFactory.VoidReactive(), options);

        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<T, Task<T2>> taskFactory)
            => new TransformBlock<T, T2>(taskFactory);

        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<T, Task<T2>> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, T2>(taskFactory, options);


        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<Task<T2>> taskFactory)
            => new TransformBlock<T, T2>(_ => taskFactory());

        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<Task<T2>> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, T2>(_ => taskFactory(), options);

        public static async Task<Unit> VoidReactive(this Task This) {
            await This.ConfigureAwait(false);
            return Unit.Default;
        }

        public static async Task<Unit> VoidReactive(this Func<Task> This) {
            await This().ConfigureAwait(false);
            return Unit.Default;
        }

        public static Func<T, Task<Unit>> VoidReactive<T>(this Func<T, Task> This) => arg => This(arg).VoidReactive();
    }
}