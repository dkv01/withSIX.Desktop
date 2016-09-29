// <copyright company="SIX Networks GmbH" file="QueueHubMessenger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api.Messengers
{
    public class QueueHubMessenger : IInfrastructureService, IQueueHubMessenger
    {
        readonly IHubContext<QueueHub, IQueueClientHub> _hubContext =
            SignalrOwinModule.ConnectionManager.GetHubContext<QueueHub, IQueueClientHub>();

        public Task AddToQueue(QueueItem item) => _hubContext.Clients.All.Added(item);

        public Task RemoveFromQueue(Guid id) => _hubContext.Clients.All.Removed(id);

        public Task Update(QueueItem item)
            => _hubContext.Clients.All.Updated(new QueueUpdate {Id = item.Id, Item = item});
    }
}