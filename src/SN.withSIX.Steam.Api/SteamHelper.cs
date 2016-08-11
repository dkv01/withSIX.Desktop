// <copyright company="SIX Networks GmbH" file="SteamHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Steamworks;

namespace SN.withSIX.Steam.Api
{
    public static class SteamHelper
    {
        internal static EventLoopScheduler Scheduler { get; set; }

        public static IObservable<Unit> FromCancellationToken(this CancellationToken cancelToken)
            => Observable.Create<Unit>(observer => cancelToken.Register(() => {
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
            })).ObserveOn(Scheduler);

        public static IObservable<T> CreateObservableFromCallback<T>(CancellationToken cancelToken)
            => Observable.Create<T>(observer => {
                var callback = Callback<T>.Create(observer.OnNext);
                var r = cancelToken.Register(() => HandleCanceled(observer));
                return () => {
                    callback.Unregister();
                    r.Dispose();
                };
            }).ObserveOn(Scheduler);

        public static IObservable<T> CreateObservableFromCallback<T>()
            => Observable.Create<T>(observer => {
                var callback = Callback<T>.Create(observer.OnNext);
                return callback.Unregister;
            }).ObserveOn(Scheduler);

        public static IObservable<T> CreateObservableFromCallresults<T>(this SteamAPICall_t apiCall)
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
            }).ObserveOn(Scheduler);

        public static IObservable<T> CreateObservableFromCallresults<T>(this SteamAPICall_t apiCall,
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
                        HandleCanceled(observer);
                    }
                });
                return () => {
                    if (callback.IsActive())
                        callback.Cancel();
                    if (!cancelToken.IsCancellationRequested)
                        r.Dispose();
                };
            }).ObserveOn(Scheduler);

        private static void HandleCanceled<T>(IObserver<T> observer) {
            try {
                observer.OnError(new OperationCanceledException());
            } catch (OperationCanceledException) {}
        }
    }
}