// <copyright company="SIX Networks GmbH" file="GameLockMonitor.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Core.Games.Services;

namespace SN.withSIX.Mini.Applications.Services
{
    public interface IGameLockMonitor
    {
        IObservable<GameLockMonitor.GameLockState> GetObservable(Guid id);
    }

    public class GameLockMonitor : IGameLockMonitor, IApplicationService
    {
        readonly Dictionary<Guid, GameLockState> _currentValues = new Dictionary<Guid, GameLockState>();
        readonly Dictionary<Guid, Subject<GameLockState>> _observables = new Dictionary<Guid, Subject<GameLockState>>();

        public GameLockMonitor(IMessageBus messageBus) {
            messageBus.Listen<GameLockChanged>()
                .Subscribe(Handle);
        }

        public IObservable<GameLockState> GetObservable(Guid gameId) {
            var result = GetOrAdd(gameId);
            return result.Item1.StartWith(result.Item2);
        }

        Tuple<Subject<GameLockState>, GameLockState> GetOrAdd(Guid gameId) {
            lock (_observables) {
                if (!_observables.ContainsKey(gameId)) {
                    _observables[gameId] = new Subject<GameLockState>();
                    _currentValues[gameId] = new GameLockState(false, false);
                }
                return Tuple.Create(_observables[gameId], _currentValues[gameId]);
            }
        }

        void Handle(GameLockChanged gameLockChanged) {
            var obs = GetOrAdd(gameLockChanged.GameId);
            lock (obs) {
                var state = new GameLockState(gameLockChanged.IsLocked, gameLockChanged.CanAbort);
                _currentValues[gameLockChanged.GameId] = state;
                obs.Item1.OnNext(state);
            }
        }

        public class GameLockState
        {
            public GameLockState(bool isLocked, bool canAbort) {
                IsLocked = isLocked;
                CanAbort = canAbort;
            }

            public bool IsLocked { get; }
            public bool CanAbort { get; }
        }
    }
}