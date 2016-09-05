﻿// <copyright company="SIX Networks GmbH" file="GTAGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.GTA.Models
{
    public abstract class GTAGame : BasicGame
    {
        protected GTAGame(Guid id, GameSettings settings) : base(id, settings) {}

        protected override InstallContentAction GetInstallAction(
            IDownloadContentAction<IInstallableContent> action)
            => new InstallContentAction(action.Content, action.CancelToken) {
                RemoteInfo = RemoteInfo,
                Paths = ContentPaths,
                Game = this,
                CheckoutType = CheckoutType.CheckoutWithoutRemoval,
                GlobalWorkingPath = InstalledState.Directory,
                Cleaning = ContentCleaning,
                Force = action.Force,
                HideLaunchAction = action.HideLaunchAction,
                Name = action.Name
            };
    }
}