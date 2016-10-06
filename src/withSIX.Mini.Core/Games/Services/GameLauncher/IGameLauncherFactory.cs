// <copyright company="SIX Networks GmbH" file="IGameLauncherFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Mini.Core.Games.Services.GameLauncher
{
    //[ContractClass(typeof (ContractClassForGameLauncherFactory))]
    public interface IGameLauncherFactory
    {
        T Create<T>(ILaunchWith<T> game) where T : class, IGameLauncher;
    }

    public interface IServerQueryFactory
    {
        T Create<T>(IServerQueryWith<T> game) where T : class, IServerQuery;
    }

    public interface IServerQuery {}

    public interface IServerQueryWith<T> {}

    /*    [ContractClassFor(typeof (IGameLauncherFactory))]
    public abstract class ContractClassForGameLauncherFactory : IGameLauncherFactory
    {
        public T Create<T>(ILaunchWith<T> game) where T : class, IGameLauncher {
            Contract.Requires<ArgumentNullException>(game != null);
            return default(T);
        }
    }
    */
}