// <copyright company="SIX Networks GmbH" file="Extensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using NDepend.Path;
using Steamworks;
using withSIX.Steam.Api.Services;

namespace withSIX.Steam.Api.Helpers
{
    public static class Extensions
    {
        public static IObservable<Unit> Execute(this IScheduler This, Action act)
            => Observable.Return(Unit.Default, This)
                .Do(_ => act());

        public static IObservable<T> CreateErrorSource<T>(this IObservable<Exception> ex)
            => Observable.Create<T>(o => ex.Subscribe(o.OnError));

        public static IObservable<T> ConnectErrorSource<T>(this IObservable<T> src, IObservable<Exception> errorSource)
            => src.Merge(errorSource.CreateErrorSource<T>());

        public static bool RequiresDownloading(this EItemState state) => !state.IsInstalled() || state.NeedsUpdate();

        public static bool IsSubscribed(this EItemState state) => state.HasFlag(EItemState.k_EItemStateSubscribed);
        public static bool IsInstalled(this EItemState state) => state.HasFlag(EItemState.k_EItemStateInstalled);
        public static bool NeedsUpdate(this EItemState state) => state.HasFlag(EItemState.k_EItemStateNeedsUpdate);
        public static bool IsLegacy(this EItemState state) => state.HasFlag(EItemState.k_EItemStateLegacyItem);

        public static IObservable<Unit> FromCancellationToken(this CancellationToken cancelToken, ISteamSession session)
            => Observable.Create<Unit>(observer => cancelToken.Register(() => {
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
            })).ObserveOn(session.Scheduler);

        public static IObservable<T> CreateObservableFromCallresults<T>(this SteamAPICall_t apiCall,
                ISteamSession session)
            => Observable.Create<T>(observer => {
                var callback = CallResult<T>.Create((cb, fail) => {
                    if (fail)
                        observer.OnError(new Exception("Failed to complete operation"));
                    else
                        observer.OnNext(cb);
                });
                callback.Set(apiCall);
                return () => {
                    if (callback.IsActive())
                        callback.Cancel();
                };
            }).ObserveOn(session.Scheduler);

        public static IObservable<T> CreateObservableFromCallresults<T>(this SteamAPICall_t apiCall,
                ISteamSession session,
                CancellationToken cancelToken)
            => Observable.Create<T>(observer => {
                var callback = CallResult<T>.Create((cb, fail) => {
                    if (fail)
                        observer.OnError(new Exception("Failed to complete operation"));
                    else
                        observer.OnNext(cb);
                });
                callback.Set(apiCall);
                var r = cancelToken.Register(() => {
                    try {
                        if (callback.IsActive())
                            callback.Cancel();
                    } finally {
                        observer.HandleCanceled();
                    }
                });
                return () => {
                    if (callback.IsActive())
                        callback.Cancel();
                    if (!cancelToken.IsCancellationRequested)
                        r.Dispose();
                };
            }).ObserveOn(session.Scheduler);

        public static void HandleCanceled<T>(this IObserver<T> observer) {
            try {
                observer.OnError(new OperationCanceledException());
            } catch (OperationCanceledException) {}
        }

        public static IAbsolutePath GetLocation(this ItemInstallInfo This, bool isLegacy)
            => isLegacy ? (IAbsolutePath) This.Location.ToAbsoluteFilePath() : This.Location.ToAbsoluteDirectoryPath();

        public static IObservable<Unit> Void<T>(this IObservable<T> This) => This.Select(x => Unit.Default);
    }
}