// <copyright company="SIX Networks GmbH" file="IGameLauncherFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Mini.Core.Games.Services.GameLauncher
{
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
}