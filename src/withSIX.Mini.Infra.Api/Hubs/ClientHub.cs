// <copyright company="SIX Networks GmbH" file="ClientHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Hubs;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Features.Settings;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class ClientHub : HubBase<IClientClientHub>
    {
        public Task<ClientInfo> GetInfo() => Send(new GetClientInfo());

        public Task SetLogin(string apiKey) => Send(new SetLogin(apiKey));

        public Task<string> BrowseFolderDialog(FolderDialogOptions options) => Send(new BrowseFolder(options));

        [Obsolete]
        public async Task ConfirmPremium() {}

        public Task ResolveUserError(ResolveUserError command) => Send(command);

        public Task Login(AccessInfo info) => Send(new Login(info));

        public Task PerformUpdate() => Send(new PerformUpdate());

        public Task InstallExplorerExtension() => Send(new InstallExtension());
        public Task UninstallExplorerExtension() => Send(new RemoveExtension());
    }

    public interface IClientClientHub
    {
        void AppStateUpdated(ClientInfo appState);
        void UserErrorResolved(UserErrorResolved notification);
        void UserErrorAdded(UserErrorAdded notification);
    }
}