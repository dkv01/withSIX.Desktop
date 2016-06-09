// <copyright company="SIX Networks GmbH" file="IQueueHubMessenger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;

namespace SN.withSIX.Mini.Applications.Services.Infra
{
    public interface IQueueHubMessenger
    {
        Task AddToQueue(QueueItem item);
        Task RemoveFromQueue(Guid id);
        Task Update(QueueItem item);
    }
}