﻿// <copyright company="SIX Networks GmbH" file="GameLauncherFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Applications.Factories;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Applications.Factories
{
    class GameLauncherFactory : AbstractFactory, IGameLauncherFactory, IApplicationService
    {
        public GameLauncherFactory(IDepResolver depResolver) : base(depResolver) {}
        public T Create<T>(ILaunchWith<T> game) where T : class, IGameLauncher => GetInstance<T>();
    }
}