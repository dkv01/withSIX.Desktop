﻿// <copyright company="SIX Networks GmbH" file="QueueHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public class QueueHub : HubBase<IQueueClientHub>
    {
        public Task<QueueInfo> GetQueueInfo() => SendAsync(new GetQueue());

        public Task Retry(Guid id) => SendAsync(new RetryQueueItem(id));

        public Task Cancel(Guid id) => SendAsync(new CancelQueueItem(id));

        public Task Pause(Guid id) {
            throw new NotImplementedException();
        }

        public Task Remove(Guid id) => SendAsync(new RemoveQueueItem(id));
    }

    public interface IQueueClientHub
    {
        Task Updated(QueueUpdate update);
        Task Removed(Guid id);
        Task Added(QueueItem item);
    }
}