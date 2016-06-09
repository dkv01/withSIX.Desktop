// <copyright company="SIX Networks GmbH" file="ServerHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Mini.Applications.Usecases.Main.Servers;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public class ServerHub : HubBase<IServerHubClient>
    {
        public Task<ServersList> GetServers(GetServersQuery info) => RequestAsync(new GetServers(info));

        public Task<ServersInfo> GetServersInfo(GetServerQuery info) => RequestAsync(new GetServersInfo(info));

        public Task<ServersInfo> QueryServers(GetServersInfoQuery info) => RequestAsync(new QueryServersInfo(info));
    }

    public interface IServerHubClient {}
}