// <copyright company="SIX Networks GmbH" file="GameLockMonitor.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Core.Games.Services;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public class GameLockMonitor : IGameLockMonitor, IInfrastructureService
    {
        readonly Dictionary<Guid, GameLockState> _currentValues = new Dictionary<Guid, GameLockState>();
        readonly Dictionary<Guid, Subject<GameLockState>> _observables = new Dictionary<Guid, Subject<GameLockState>>();

        public GameLockMonitor(IGameLocker gameLocker) {
            gameLocker.LockChanged.Subscribe(Handle);
        }

        public Task<GameLockState> GetObservable(Guid gameId) {
            var result = GetOrAdd(gameId);
            return result.Item1.StartWith(result.Item2).FirstAsync().ToTask();
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
    }
}