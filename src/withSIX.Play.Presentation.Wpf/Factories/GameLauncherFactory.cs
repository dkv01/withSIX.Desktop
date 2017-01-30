// <copyright company="SIX Networks GmbH" file="GameLauncherFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using withSIX.Core.Applications.Factories;
using withSIX.Core.Presentation;
using withSIX.Play.Core.Games.Services.GameLauncher;

namespace withSIX.Play.Presentation.Wpf.Factories
{
    class GameLauncherFactory : IGameLauncherFactory, IPresentationService
    {
        readonly IDepResolver _depResolver;

        public GameLauncherFactory(IDepResolver depResolver) {
            if (depResolver == null) throw new ArgumentNullException(nameof(depResolver));
            _depResolver = depResolver;
        }

        public T Create<T>(ILaunchWith<T> game) where T : class, IGameLauncher => _depResolver.GetInstance<T>();
    }
}