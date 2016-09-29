// <copyright company="SIX Networks GmbH" file="ServerHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api.Messengers
{
    public class ServerHandler : IAsyncNotificationHandler<ServersPageReceived>
    {
        private readonly Lazy<IHubContext<ServerHub, IServerHubClient>> _hubContext = SystemExtensions.CreateLazy(() =>
            SignalrOwinModule.ConnectionManager.GetHubContext<ServerHub, IServerHubClient>());

        public Task Handle(ServersPageReceived notification)
            => _hubContext.Value.Clients.All.ServersPageReceived(notification);
    }
}