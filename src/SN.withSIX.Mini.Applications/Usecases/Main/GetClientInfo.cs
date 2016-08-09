// <copyright company="SIX Networks GmbH" file="GetClientInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class GetClientInfo : IAsyncQuery<ClientInfo> {}

    public class GetClientInfoHandler : IAsyncRequestHandler<GetClientInfo, ClientInfo>
    {
        private readonly IStateHandler _stateHandler;

        public GetClientInfoHandler(IStateHandler stateHandler) {
            _stateHandler = stateHandler;
        }

        public async Task<ClientInfo> Handle(GetClientInfo request) => _stateHandler.ClientInfo;
    }

    public class ClientInfo
    {
        public ClientInfo(AppState state, bool extensionInstalled) {
            UpdateState = state.UpdateState;
            NewVersionAvailable = state.Version?.ToString();
            ExtensionInstalled = extensionInstalled;
        }

        public AppUpdateState UpdateState { get; }
        public string ApiVersion { get; } = Consts.ApiVersion;
        public string Version { get; } = Consts.ProductVersion;
        public string NewVersionAvailable { get; }
        public bool ExtensionInstalled { get; }
    }
}