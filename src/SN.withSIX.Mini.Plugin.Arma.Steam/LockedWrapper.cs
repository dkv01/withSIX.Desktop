using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core;

namespace withSIX.Steam.Plugin.Arma
{
    public abstract class LockedWrapper {
        public static ISafeCallFactory callFactory { get; set; }
    }

    public class LockedWrapper<T> : LockedWrapper where T : class
    {
        private readonly T _obj;
        private readonly IScheduler _scheduler;
        private readonly ISafeCall _safeCall;

        public IScheduler Scheduler => _scheduler;

        public LockedWrapper(T obj, IScheduler scheduler) {
            if (obj == null) {
                throw new ArgumentNullException("obj");
            }
            _safeCall = callFactory.Create();
            _obj = obj;
            _scheduler = scheduler;
        }

        public async Task Do(Action<T> action) => await Observable.Return(Unit.Default, _scheduler)
            .Do(_ => _safeCall.Do(() => action(_obj)));

        public async Task<TResult> Do<TResult>(Func<T, TResult> action)
            => await Observable.Return(Unit.Default, _scheduler)
                .Select(_ => _safeCall.Do(() => action(_obj)));

        public void DoWithoutLock(Action<T> action) => _safeCall.Do(() => action(_obj));

        public TResult DoWithoutLock<TResult>(Func<T, TResult> action) => _safeCall.Do(() => action(_obj));
    }

}