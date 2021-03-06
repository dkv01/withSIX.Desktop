﻿// <copyright company="SIX Networks GmbH" file="EvilGlobalSelectedGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Caliburn.Micro;

using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Services;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy.Events;
using withSIX.Play.Core.Options;
using withSIX.Play.Core.Options.Entries;
using withSIX.Api.Models.Extensions;
using PropertyChangedBase = withSIX.Core.Helpers.PropertyChangedBase;

namespace withSIX.Play.Core.Games.Legacy
{
    [Obsolete("Destroy")]
    public class EvilGlobalSelectedGame : PropertyChangedBase, IDomainService
    {
        readonly UserSettings _settings;
        Game _activeGame;
        bool _first;

        public EvilGlobalSelectedGame(UserSettings settings) {
            _settings = settings;
        }

        public Game ActiveGame
        {
            get { return _activeGame; }
            set
            {
                if (_activeGame == value)
                    return;
                _activeGame = value;

                if (_first)
                    HandleGameSetChanged(value);
                else {
                    _first = true;
                    OnPropertyChanged();
                }
            }
        }

        async void HandleGameSetChanged(Game x) {
            _settings.Ready = false;
            _settings.GameOptions.RecentGameSet = x == null ? null : new RecentGameSet(x);
            OnPropertyChanged(nameof(ActiveGame));
            await
                TaskExt.StartLongRunningTask(() => TryActiveGameChanged(x))
                    .ConfigureAwait(false);
        }

        async Task TryActiveGameChanged(Game x) {
            try {
                x.RefreshState();
                await CalculatedGameSettings.NotifyEnMass(new ActiveGameChanged(x)).ConfigureAwait(false);
                await CalculatedGameSettings.NotifyEnMass(new ActiveGameChangedForReal(x)).ConfigureAwait(false);
            } finally {
                _settings.Ready = true;
            }
        }
    }

    public class ActiveGameChangedForReal : ISyncDomainEvent
    {
        public ActiveGameChangedForReal(Game game) {
            Game = game;
        }

        public Game Game { get; }
    }
}