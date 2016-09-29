// <copyright company="SIX Networks GmbH" file="LockedWrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using withSIX.Steam.Core;

namespace withSIX.Steam.Plugin.Arma
{
    public abstract class LockedWrapper
    {
        public static ISafeCallFactory callFactory { get; set; }
    }

    public class LockedWrapper<T> : LockedWrapper where T : class
    {
        private readonly T _obj;
        private readonly ISafeCall _safeCall;

        public LockedWrapper(T obj, IScheduler scheduler) {
            if (obj == null) {
                throw new ArgumentNullException("obj");
            }
            _safeCall = callFactory.Create();
            _obj = obj;
            Scheduler = scheduler;
        }

        public IScheduler Scheduler { get; }

        public async Task Do(Action<T> action) => await Observable.Return(Unit.Default, Scheduler)
            .Do(_ => _safeCall.Do(() => action(_obj)));

        public async Task<TResult> Do<TResult>(Func<T, TResult> action)
            => await Observable.Return(Unit.Default, Scheduler)
                .Select(_ => _safeCall.Do(() => action(_obj)));

        public void DoWithoutLock(Action<T> action) => _safeCall.Do(() => action(_obj));

        public TResult DoWithoutLock<TResult>(Func<T, TResult> action) => _safeCall.Do(() => action(_obj));
    }
}