// <copyright company="SIX Networks GmbH" file="ClientHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Hubs;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Applications.Usecases.Main;
using withSIX.Mini.Applications.Usecases.Settings;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class ClientHub : HubBase<IClientClientHub>
    {
        public Task<ClientInfo> GetInfo() => SendAsync(new GetClientInfo());

        public Task SetLogin(string apiKey) => SendAsync(new SetLogin(apiKey));

        public Task<string> BrowseFolderDialog(FolderDialogOptions options) => SendAsync(new BrowseFolder(options));

        [Obsolete]
        public async Task ConfirmPremium() {}

        public Task ResolveUserError(ResolveUserError command) => SendAsync(command);

        public Task Login(AccessInfo info) => SendAsync(new Applications.Usecases.Main.Login(info));

        public Task PerformUpdate() => SendAsync(new PerformUpdate());

        public Task InstallExplorerExtension() => SendAsync(new InstallExtension());
        public Task UninstallExplorerExtension() => SendAsync(new RemoveExtension());
    }

    public interface IClientClientHub : IHub
    {
        void AppStateUpdated(ClientInfo appState);
        void UserErrorResolved(UserErrorResolved notification);
        void UserErrorAdded(UserErrorAdded notification);
    }
}