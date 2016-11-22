// <copyright company="SIX Networks GmbH" file="GameLocker.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace withSIX.Mini.Core.Games.Services
{
    public class GameLockInfo : IDisposable
    {
        private readonly IDisposable _disp;

        public GameLockInfo(IDisposable disp, CancellationToken token) {
            Token = token;
            _disp = disp;
        }

        public CancellationToken Token { get; }

        public void Dispose() {
            _disp.Dispose();
        }
    }

    public interface IGameLocker
    {
        IObservable<GameLockChanged> LockChanged { get; }
        Task<CancellationTokenRegistration> RegisterCancel(Guid gameId, Action cancelAction);
        Task Cancel(Guid gameId);
        Task<GameLockInfo> ConfirmLock(Guid gameId, bool canAbort = false);
        void ReleaseLock(Guid gameId);
        Task Cancel();
    }

    public class GameLockChanged
    {
        public GameLockChanged(Guid gameId, bool isLocked, bool canCancel = false) {
            GameId = gameId;
            IsLocked = isLocked;
            CanAbort = canCancel;
        }

        public Guid GameId { get; }
        public bool IsLocked { get; }
        public bool CanAbort { get; }
    }

    public class NotLockedException : Exception {}
}