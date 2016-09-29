// <copyright company="SIX Networks GmbH" file="ServerHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNet.SignalR;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api.Messengers
{
    public class ServerHandler : IAsyncNotificationHandler<ServersPageReceived>
    {
        private readonly IHubContext<IServerHubClient> _hubContext =
            GlobalHost.ConnectionManager.GetHubContext<ServerHub, IServerHubClient>();

        public Task Handle(ServersPageReceived notification)
            => _hubContext.Clients.All.ServersPageReceived(notification);
    }
}