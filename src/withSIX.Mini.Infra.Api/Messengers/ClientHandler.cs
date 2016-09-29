// <copyright company="SIX Networks GmbH" file="ClientHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api.Messengers
{
    public class ClientHandler : INotificationHandler<ClientInfoUpdated>, INotificationHandler<UserErrorResolved>,
        INotificationHandler<UserErrorAdded>
    {
        readonly IHubContext<ClientHub, IClientClientHub> _hubContext =
            SignalrOwinModule.ConnectionManager.GetHubContext<ClientHub, IClientClientHub>();
        private readonly IStateHandler _stateHandler;

        public ClientHandler(IStateHandler stateHandler) {
            _stateHandler = stateHandler;
        }

        public void Handle(ClientInfoUpdated notification) {
            _hubContext.Clients.All.AppStateUpdated(_stateHandler.ClientInfo);
        }

        public void Handle(UserErrorAdded notification) => _hubContext.Clients.All.UserErrorAdded(notification);

        public void Handle(UserErrorResolved notification) => _hubContext.Clients.All.UserErrorResolved(notification);
    }
}