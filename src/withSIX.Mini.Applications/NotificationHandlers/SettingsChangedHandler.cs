﻿// <copyright company="SIX Networks GmbH" file="SettingsChangedHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Applications.Usecases;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Applications.NotificationHandlers
{
    public class SettingsChangedHandler : DbCommandBase, IAsyncNotificationHandler<SettingsUpdated>
    {
        readonly StartWithWindowsHandler _startWithWindowsHandler = new StartWithWindowsHandler();

        public SettingsChangedHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task Handle(SettingsUpdated notification) {
            _startWithWindowsHandler.HandleStartWithWindows(notification.Subject.Local.StartWithWindows);
        }
    }
}