// <copyright company="SIX Networks GmbH" file="GameLocker.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Core.Games.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using Disposable = withSIX.Core.Helpers.Disposable;

namespace withSIX.Mini.Infra.Data.Services
{
    // TODO: Fix this retarded implementation!!!
    public class GameLocker : IDisposable, IGameLocker, IInfrastructureService
    {
        private readonly CompositeDisposable _dsp;
        readonly IDictionary<Guid, CancellationTokenSource> _list = new Dictionary<Guid, CancellationTokenSource>();
        readonly AsyncLock _lock = new AsyncLock();
        private readonly Subject<GameLockChanged> _lockChanged = new Subject<GameLockChanged>();

        public GameLocker() {
            _dsp = new CompositeDisposable {
                _lock,
                _lockChanged
            };
        }

        public void Dispose() => _dsp.Dispose();

        public IObservable<GameLockChanged> LockChanged => _lockChanged.AsObservable();

        public async Task<CancellationTokenRegistration> RegisterCancel(Guid gameId, Action cancelAction) {
            using (await _lock.LockAsync().ConfigureAwait(false))
                return RegisterCancelInternal(gameId, cancelAction);
        }

        public async Task<GameLockInfo> ConfirmLock(Guid gameId, bool canAbort = false) {
            using (await _lock.LockAsync().ConfigureAwait(false))
                return new GameLockInfo(new Disposable(() => ReleaseLock(gameId)), ConfirmLockInternal(gameId, canAbort));
        }

        public void ReleaseLock(Guid gameId) {
            using (_lock.Lock())
                ReleaseLockInternal(gameId);
        }

        public async Task Cancel() {
            IObservable<Unit> t;
            using (await _lock.LockAsync().ConfigureAwait(false))
                t = CancelInternal();
            await t;
        }

        public async Task Cancel(Guid gameId) {
            Task t;
            using (await _lock.LockAsync().ConfigureAwait(false))
                t = CancelInternal(gameId);
            await t;
        }

        /*
        public static async Task Busy(Func<Task> act) {
            await StatusChange(Status.Preparing, ProgressInfo.Default).ConfigureAwait(false);
            try {
                await act().ConfigureAwait(false);
            } finally {
                await StatusChange(Status.Synchronized, ProgressInfo.Default).ConfigureAwait(false);
            }
        }

        public static async Task<T> Busy<T>(Func<Task<T>> act) {
            await StatusChange(Status.Preparing, ProgressInfo.Default).ConfigureAwait(false);
            try {
                return await act().ConfigureAwait(false);
            } finally {
                await StatusChange(Status.Synchronized, ProgressInfo.Default).ConfigureAwait(false);
            }
        }
        */

        private static Task StatusChange(Status status, ProgressInfo info) => new StatusChanged(status, info).Raise();

        private CancellationTokenRegistration RegisterCancelInternal(Guid gameId, Action cancelAction) {
            var cts = GetCts(gameId);
            if (cts.IsCancellationRequested)
                throw new OperationCanceledException("The user cancelled the operation");
            var reg = cts.Token.Register(cancelAction);
            _lockChanged.OnNext(new GameLockChanged(gameId, true, true));
            return reg;
        }

        private Task CancelInternal(Guid gameId) {
            var cts = GetCts(gameId);
            if (cts.IsCancellationRequested)
                return GenerateObservable(gameId).Select(x => Unit.Value).ToTask();
            return Task.Run(async () => {
                _lockChanged.OnNext(new GameLockChanged(gameId, true, false));
                cts.Cancel();
                await GenerateObservable(gameId).Select(x => Unit.Value);
            });
        }

        private IObservable<GameLockChanged> GenerateObservable(Guid gameId)
            => LockChanged.Where(x => (x.GameId == gameId) && !x.IsLocked).Take(1);

        private CancellationToken ConfirmLockInternal(Guid gameId, bool canAbort) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Locking {gameId}");
            if (_list.ContainsKey(gameId))
                throw new AlreadyLockedException();
            var cancellationTokenSource = new CancellationTokenSource();
            _list.Add(gameId, cancellationTokenSource);
            _lockChanged.OnNext(new GameLockChanged(gameId, true, canAbort));
            return cancellationTokenSource.Token;
        }

        private IObservable<Unit> CancelInternal() {
            var list = new List<IObservable<GameLockChanged>>();
            foreach (var l in _list) {
                l.Value.Cancel();
                _lockChanged.OnNext(new GameLockChanged(l.Key, true, false));
                list.Add(GenerateObservable(l.Key));
            }
            return list.Any() ? list.Merge().Select(x => Unit.Value) : Observable.Return(Unit.Value);
        }

        private CancellationTokenSource GetCts(Guid gameId) {
            var cts = _list[gameId];
            if (cts == null)
                throw new NotLockedException();
            return cts;
        }

        private async Task ReleaseLockAsync(Guid gameId) {
            using (await _lock.LockAsync().ConfigureAwait(false))
                ReleaseLockInternal(gameId);
        }

        private void ReleaseLockInternal(Guid gameId) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Unlocking {gameId}");
            var cts = GetCts(gameId);
            cts.Dispose();
            _list.Remove(gameId);
            _lockChanged.OnNext(new GameLockChanged(gameId, false));
        }
    }
}