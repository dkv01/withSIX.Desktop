// <copyright company="SIX Networks GmbH" file="ObservableExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SN.withSIX.Core.Applications.Extensions
{
    public static class ObservableExtensions
    {
        public static IObservable<T2> MergeTask<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory)
            => obs.SelectMany(x => Observable.FromAsync(taskFactory));

        public static IObservable<T2> MergeTask<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory,
                int degreeOfParallism)
            => obs.SelectTask(taskFactory).Merge(degreeOfParallism);

        public static IObservable<T2> ConcatTask<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory)
            => obs.SelectTask(taskFactory).Concat();

        public static IObservable<IObservable<T2>> SelectTask<T, T2>(this IObservable<T> obs, Func<Task<T2>> taskFactory)
            => obs.Select(x => Observable.FromAsync(taskFactory));

        public static IObservable<T2> MergeTask<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory,
                int degreeOfParallism)
            => obs.SelectTask(taskFactory).Merge(degreeOfParallism);

        public static IObservable<T2> MergeTask<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory)
            => obs.SelectTask(taskFactory).Merge();

        public static IObservable<T2> ConcatTask<T, T2>(this IObservable<T> obs, Func<T, Task<T2>> taskFactory)
            => obs.SelectTask(taskFactory).Concat();

        public static IObservable<IObservable<T2>> SelectTask<T, T2>(this IObservable<T> obs,
            Func<T, Task<T2>> taskFactory) => obs.Select(x => Observable.FromAsync(() => taskFactory(x)));


        public static IObservable<Unit> Void<T>(this IObservable<T> This) => This.Select(x => Unit.Default);

        public static IObservable<T> MergeTask<T>(this IObservable<T> obs, Func<T, Task<T>> taskFactory,
                int degreeOfParallism)
            => obs.SelectTask(taskFactory).Merge(degreeOfParallism); // Similar to SelectMany?

        public static IObservable<Unit> MergeTask<T>(this IObservable<T> obs, Func<Task> taskFactory,
                int degreeOfParallism)
            => obs.SelectTask(taskFactory).Merge(degreeOfParallism); // Similar to SelectMany?

        public static IObservable<Unit> ConcatTask<T>(this IObservable<T> obs, Func<Task> taskFactory)
            => obs.SelectTask(taskFactory).Concat();

        public static IObservable<IObservable<Unit>> SelectTask<T>(this IObservable<T> obs, Func<Task> taskFactory)
            => obs.Select(x => Observable.FromAsync(taskFactory));

        public static IObservable<Unit> MergeTask<T>(this IObservable<T> obs, Func<T, Task> taskFactory,
                int degreeOfParallism)
            => obs.SelectTask(taskFactory).Merge(degreeOfParallism);

        public static IObservable<Unit> ConcatTask<T>(this IObservable<T> obs, Func<T, Task> taskFactory)
            => obs.SelectTask(taskFactory).Concat();

        public static IObservable<IObservable<Unit>> SelectTask<T>(this IObservable<T> obs,
            Func<T, Task> taskFactory) => obs.Select(x => Observable.FromAsync(() => taskFactory(x)));
    }
}