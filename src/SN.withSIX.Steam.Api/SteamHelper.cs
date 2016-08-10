// <copyright company="SIX Networks GmbH" file="SteamHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Steamworks;

namespace SN.withSIX.Steam.Api
{
    public static class SteamHelper
    {
        public static IObservable<Unit> FromCancellationToken(this CancellationToken cancelToken)
            => Observable.Create<Unit>(observer => cancelToken.Register(() =>
            {
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
            }));

        public static IObservable<T> CreateObservableFromCallback<T>(CancellationToken cancelToken)
            => Observable.Create<T>(observer =>
            {
                var callback = Callback<T>.Create(observer.OnNext);
                var r = cancelToken.Register(() =>
                {
                    try
                    {
                        throw new OperationCanceledException();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                });
                return () => {
                    callback.Unregister();
                    r.Dispose();
                };
            });

        public static IObservable<T> CreateObservableFromCallback<T>()
            => Observable.Create<T>(observer => {
                var callback = Callback<T>.Create(observer.OnNext);
                return callback.Unregister;
            });

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
            });

        public static IObservable<T> CreateObservableFromCallresults<T>(this SteamAPICall_t apiCall,
            CancellationToken cancelToken)
            => Observable.Create<T>(observer =>
            {
                var callback = CallResult<T>.Create((cb, fail) =>
                {
                    if (fail)
                        observer.OnError(new Exception("Failed to complete operation"));
                    else
                        observer.OnNext(cb);
                });
                callback.Set(apiCall);
                var r = cancelToken.Register(() =>
                {
                    try
                    {
                        if (callback.IsActive())
                            callback.Cancel();
                        throw new OperationCanceledException();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                });
                return () =>
                {
                    if (callback.IsActive())
                        callback.Cancel();
                    r.Dispose();
                };
            });
    }
}