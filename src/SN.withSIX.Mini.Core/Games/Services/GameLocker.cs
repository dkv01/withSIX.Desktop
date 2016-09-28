// <copyright company="SIX Networks GmbH" file="GameLocker.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SN.withSIX.Mini.Core.Games.Services
{
    public class Info : IDisposable
    {
        private readonly IDisposable _disp;

        public Info(IDisposable disp, CancellationToken token) {
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
        Task<CancellationTokenRegistration> RegisterCancel(Guid gameId, Action cancelAction);
        Task Cancel(Guid gameId);
        Task<Info> ConfirmLock(Guid gameId, bool canAbort = false);
        void ReleaseLock(Guid gameId);
        Task Cancel();
        IObservable<GameLockChanged> LockChanged { get; }
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