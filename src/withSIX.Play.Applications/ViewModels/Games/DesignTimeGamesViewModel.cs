﻿// <copyright company="SIX Networks GmbH" file="DesignTimeGamesViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using Caliburn.Micro;
using MediatR;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Play.Applications.Services;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Applications.ViewModels.Games.Overlays;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class DesignTimeGamesViewModel : GamesViewModel, IDesignTimeViewModel
    {
        public DesignTimeGamesViewModel()
            : base(IoC.Get<IMediator>(),
                IoC.Get<GameInfoOverlayViewModel>(),
                IoC.Get<IGameContext>(),
                IoC.Get<UserSettings>(),
                new Lazy<LaunchManager>()) {}
    }
}