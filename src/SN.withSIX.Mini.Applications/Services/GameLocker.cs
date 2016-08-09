// <copyright company="SIX Networks GmbH" file="GameLocker.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using MediatR;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Core.Games.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using Disposable = SN.withSIX.Core.Helpers.Disposable;

namespace SN.withSIX.Mini.Applications.Services
{
    public class GameLocker : IDisposable, IGameLocker, IApplicationService
    {
        private readonly CompositeDisposable _dsp;
        readonly IDictionary<Guid, CancellationTokenSource> _list = new Dictionary<Guid, CancellationTokenSource>();
        readonly AsyncLock _lock = new AsyncLock();
        private readonly IMessageBus _messageBus;

        public GameLocker(IMessageBus messageBus) {
            _messageBus = messageBus;
            var glc = _messageBus.Listen<GameLockChanged>();
            _dsp = new CompositeDisposable {
                /*
                glc
                    .Where(x => !x.IsLocked)
                    .ConcatTask(() => StatusChange(Status.Synchronized, ProgressInfo.Default)))
                    .Subscribe(),
                glc.Where(x => x.IsLocked)
                    .ConcatTask(() => StatusChange(Status.Preparing, new ProgressInfo(Status.Preparing.ToString(), 0)))
                    .Subscribe(),
                    */
                _lock
            };
        }

        public void Dispose() => _dsp.Dispose();

        public async Task<CancellationTokenRegistration> RegisterCancel(Guid gameId, Action cancelAction) {
            using (await _lock.LockAsync().ConfigureAwait(false))
                return RegisterCancelInternal(gameId, cancelAction);
        }

        public async Task<IDisposable> ConfirmLock(Guid gameId, bool canAbort = false) {
            using (await _lock.LockAsync().ConfigureAwait(false))
                ConfirmLockInternal(gameId, canAbort);
            return new Disposable(() => ReleaseLock(gameId));
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
            IObservable<Unit> t;
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
            _messageBus.SendMessage(new GameLockChanged(gameId, true, true));
            return reg;
        }

        private IObservable<Unit> CancelInternal(Guid gameId) {
            var cts = GetCts(gameId);
            if (cts.IsCancellationRequested)
                return GenerateObservable(gameId).Select(x => Unit.Value);
            cts.Cancel();
            _messageBus.SendMessage(new GameLockChanged(gameId, true, false));
            return GenerateObservable(gameId).Select(x => Unit.Value);
        }

        private IObservable<GameLockChanged> GenerateObservable(Guid gameId)
            => _messageBus.Listen<GameLockChanged>().Where(x => x.GameId == gameId && !x.IsLocked).Take(1);

        private void ConfirmLockInternal(Guid gameId, bool canAbort) {
            if (Common.Flags.Verbose)
                MainLog.Logger.Info($"Locking {gameId}");
            if (_list.ContainsKey(gameId))
                throw new AlreadyLockedException();
            _list.Add(gameId, new CancellationTokenSource());
            _messageBus.SendMessage(new GameLockChanged(gameId, true, canAbort));
        }

        private IObservable<Unit> CancelInternal() {
            var list = new List<IObservable<GameLockChanged>>();
            foreach (var l in _list) {
                l.Value.Cancel();
                _messageBus.SendMessage(new GameLockChanged(l.Key, true, false));
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
            _messageBus.SendMessage(new GameLockChanged(gameId, false));
        }
    }
}