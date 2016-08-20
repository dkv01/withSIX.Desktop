// <copyright company="SIX Networks GmbH" file="TdfExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SN.withSIX.Core.Applications.Extensions
{
    public static class TdfExtensions
    {
        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<T, Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);

        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);


        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<T, Task> taskFactory,
            int degreeOfParallism) => obs.FromTdf(taskFactory.ToTransformBlock(BuildMaxDegreeOption(degreeOfParallism)));

        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory,
            int degreeOfParallism)
            => obs.FromTdf(taskFactory.ToTransformBlock(BuildMaxDegreeOption(degreeOfParallism)));


        public static IObservable<Unit> ConcatTaskTdf<T>(this IObservable<T> obs, Func<T, Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, 1);


        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);

        public static IObservable<T2> MergeTaskTdf<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory,
            int degreeOfParallism)
            => obs.FromTdf(taskFactory.ToTransformBlock<T, T2>(BuildMaxDegreeOption(degreeOfParallism)));

        private static ExecutionDataflowBlockOptions BuildMaxDegreeOption(int degreeOfParallism)
            => new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = degreeOfParallism
            };

        public static IObservable<T2> ConcatTaskTdf<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory)
            => obs.MergeTaskTdf(taskFactory, 1);

        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, Environment.ProcessorCount);

        public static IObservable<Unit> MergeTaskTdf<T>(this IObservable<T> obs, Func<Task> taskFactory,
            int degreeOfParallism)
            => obs.FromTdf(taskFactory.ToTransformBlock<T>(BuildMaxDegreeOption(degreeOfParallism)));

        public static IObservable<Unit> ConcatTaskTdf<T>(this IObservable<T> obs, Func<Task> taskFactory)
            => obs.MergeTaskTdf(taskFactory, 1);

        public static IObservable<TResult> FromTdf<T, TResult>(this IObservable<T> source,
            Func<IPropagatorBlock<T, TResult>> blockFactory) => Observable.Defer(() => {
                var block = blockFactory();
                source.Subscribe(block.AsObserver()); // who disposes this ?
                return block.AsObservable();
            });

        public static IObservable<TResult> FromTdf<T, TResult>(
            this IObservable<T> source,
            IPropagatorBlock<T, TResult> block) => Observable.Defer(() => {
                source.Subscribe(block.AsObserver()); // who disposes this ?
                return block.AsObservable();
            });

        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<Task<T2>> taskFactory)
            => new TransformBlock<T, T2>(_ => taskFactory());

        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<Task<T2>> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, T2>(_ => taskFactory(), options);

        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<T, Task<T2>> taskFactory)
            => new TransformBlock<T, T2>(taskFactory);

        public static TransformBlock<T, T2> ToTransformBlock<T, T2>(this Func<T, Task<T2>> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, T2>(taskFactory, options);

        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<T, Task> taskFactory)
            => new TransformBlock<T, Unit>(taskFactory.VoidReactive());


        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<T, Task> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, Unit>(taskFactory.VoidReactive(), options);

        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<Task> taskFactory)
            => new TransformBlock<T, Unit>(_ => taskFactory.VoidReactive());


        public static TransformBlock<T, Unit> ToTransformBlock<T>(this Func<Task> taskFactory,
            ExecutionDataflowBlockOptions options)
            => new TransformBlock<T, Unit>(_ => taskFactory.VoidReactive(), options);


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