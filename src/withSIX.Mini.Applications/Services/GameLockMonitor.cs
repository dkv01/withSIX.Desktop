// <copyright company="SIX Networks GmbH" file="GameLockMonitor.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;

namespace withSIX.Mini.Applications.Services
{
    public interface IGameLockMonitor
    {
        Task<GameLockState> GetObservable(Guid id);
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