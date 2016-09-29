// <copyright company="SIX Networks GmbH" file="GetQueue.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Usecases.Main
{
    public class GetQueue : IAsyncQuery<QueueInfo> {}

    public class GetQueueHandler : DbRequestBase, IAsyncRequestHandler<GetQueue, QueueInfo>
    {
        private readonly IQueueManager _queueManager;

        public GetQueueHandler(IDbContextLocator dbContextLocator, IQueueManager queueManager) : base(dbContextLocator) {
            _queueManager = queueManager;
        }

        public Task<QueueInfo> Handle(GetQueue request) => Task.FromResult(_queueManager.Queue);
    }
}