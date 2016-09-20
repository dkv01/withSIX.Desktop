// <copyright company="SIX Networks GmbH" file="IServerQueryQueue.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace GameServerQuery
{
    public interface IServerQueryQueue
    {
        IServerQueryOverallState State { get; }
        Task SyncAsync(IServer[] objects);
        Task SyncAsync(IServer[] objects, CancellationToken token);
    }
}