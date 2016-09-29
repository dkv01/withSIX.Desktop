// <copyright company="SIX Networks GmbH" file="ClientHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api.Messengers
{
    public class ClientHandler : INotificationHandler<ClientInfoUpdated>, INotificationHandler<UserErrorResolved>,
        INotificationHandler<UserErrorAdded>
    {
        private readonly Lazy<IHubContext<ClientHub, IClientClientHub>> _hubContext = SystemExtensions.CreateLazy(() =>
                SignalrOwinModule.ConnectionManager.GetHubContext<ClientHub, IClientClientHub>());
        private readonly IStateHandler _stateHandler;

        public ClientHandler(IStateHandler stateHandler) {
            _stateHandler = stateHandler;
        }

        public void Handle(ClientInfoUpdated notification) {
            _hubContext.Value.Clients.All.AppStateUpdated(_stateHandler.ClientInfo);
        }

        public void Handle(UserErrorAdded notification) => _hubContext.Value.Clients.All.UserErrorAdded(notification);

        public void Handle(UserErrorResolved notification) => _hubContext.Value.Clients.All.UserErrorResolved(notification);
    }
}