// <copyright company="SIX Networks GmbH" file="ClientHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Settings;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public class ClientHub : HubBase<IClientClientHub>
    {
        public Task<ClientInfo> GetInfo() => RequestAsync(new GetClientInfo());

        public Task SetLogin(string apiKey) => RequestAsync(new SetLogin(apiKey));

        public Task<string> BrowseFolderDialog(FolderDialogOptions options) => RequestAsync(new BrowseFolder(options));

        [Obsolete]
        public async Task ConfirmPremium() {}

        public Task ResolveUserError(ResolveUserError command) => RequestAsync(command);

        public Task Login(AccessInfo info) => RequestAsync(new Applications.Usecases.Main.Login(info));

        public Task PerformUpdate() => RequestAsync(new PerformUpdate());

        public Task InstallExplorerExtension() => RequestAsync(new InstallExtension());
        public Task UninstallExplorerExtension() => RequestAsync(new RemoveExtension());
    }

    public interface IClientClientHub
    {
        void AppStateUpdated(ClientInfo appState);
        void UserErrorResolved(UserErrorResolved notification);
        void UserErrorAdded(UserErrorAdded notification);
    }
}