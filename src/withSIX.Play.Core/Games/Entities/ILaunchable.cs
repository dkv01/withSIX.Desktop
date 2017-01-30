// <copyright company="SIX Networks GmbH" file="ILaunchable.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Services.GameLauncher;

namespace withSIX.Play.Core.Games.Entities
{
    [ContractClass(typeof (LaunchableContract))]
    public interface ILaunchable
    {
        RunningGame Running { get; }
        Task<int> Launch(IGameLauncherFactory factory);
        void RegisterRunning(Process process);
        void RegisterTermination();
    }

    [ContractClassFor(typeof (ILaunchable))]
    public abstract class LaunchableContract : ILaunchable
    {
        public Task<int> Launch(IGameLauncherFactory factory) {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return default(Task<int>);
        }

        public abstract RunningGame Running { get; }

        public void RegisterRunning(Process process) {
            if (process == null) throw new ArgumentNullException(nameof(process));
        }

        public abstract void RegisterTermination();
    }
}