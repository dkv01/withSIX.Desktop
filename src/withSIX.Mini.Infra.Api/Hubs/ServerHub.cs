// <copyright company="SIX Networks GmbH" file="ServerHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core;
using withSIX.Mini.Applications.Usecases.Main.Servers;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class ServerHub : HubBase<IServerHubClient>
    {
        public Task<BatchResult> GetServers(GetServersQuery info, Guid requestId)
            => SendAsync(new GetServers(info), requestId);

        public Task<ServersInfo> GetServersInfo(GetServerQuery info) => SendAsync(new GetServersInfo(info));

        public Task<ServersInfo> QueryServers(GetServersInfoQuery info) => SendAsync(new QueryServersInfo(info));
    }

    public interface IServerHubClient
    {
        Task ServersPageReceived(ServersPageReceived info);
    }
}