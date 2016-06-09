// <copyright company="SIX Networks GmbH" file="IQueueManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SN.withSIX.Mini.Applications.Services
{
    public interface IQueueManager
    {
        QueueInfo Queue { get; }
        Task<Guid> AddToQueue(string title, Func<Action<ProgressState>, CancellationToken, Task> task);
        Task RemoveFromQueue(Guid id);
        Task Update(QueueItem item);
        Task Cancel(Guid id);
        Task Retry(Guid id);
    }
}