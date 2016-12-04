// <copyright company="SIX Networks GmbH" file="SettingsChangedHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.NotificationHandlers
{
    public class SettingsChangedHandler : DbCommandBase, IAsyncNotificationHandler<SettingsUpdated>
    {
        readonly StartWithWindowsHandler _startWithWindowsHandler = new StartWithWindowsHandler();

        public SettingsChangedHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task Handle(SettingsUpdated notification) {
            if (Common.IsWindows)
                _startWithWindowsHandler.HandleStartWithWindows(notification.Subject.Local.StartWithWindows);
        }
    }
}