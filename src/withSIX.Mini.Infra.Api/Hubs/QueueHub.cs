// <copyright company="SIX Networks GmbH" file="QueueHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Services;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public class QueueHub : HubBase<IQueueClientHub>
    {
        public Task<QueueInfo> GetQueueInfo() => Send(new GetQueue());

        public Task Retry(Guid id) => Send(new RetryQueueItem(id));

        public Task Cancel(Guid id) => Send(new CancelQueueItem(id));

        public Task CancelByContentId(Guid contentId) => Send(new CancelQueueItemByContentId(contentId));

        public Task Pause(Guid id) {
            throw new NotImplementedException();
        }

        public Task Remove(Guid id) => Send(new RemoveQueueItem(id));
    }

    public interface IQueueClientHub
    {
        Task Updated(QueueUpdate update);
        Task Removed(Guid id);
        Task Added(QueueItem item);
    }
}