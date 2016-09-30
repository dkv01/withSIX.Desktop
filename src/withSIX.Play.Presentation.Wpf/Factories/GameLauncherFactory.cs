// <copyright company="SIX Networks GmbH" file="GameLauncherFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using SN.withSIX.Core.Applications.Factories;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Play.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Play.Presentation.Wpf.Factories
{
    class GameLauncherFactory : IGameLauncherFactory, IPresentationService
    {
        readonly IDepResolver _depResolver;

        public GameLauncherFactory(IDepResolver depResolver) {
            Contract.Requires<ArgumentNullException>(depResolver != null);
            _depResolver = depResolver;
        }

        public T Create<T>(ILaunchWith<T> game) where T : class, IGameLauncher => _depResolver.GetInstance<T>();
    }
}