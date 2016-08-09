// <copyright company="SIX Networks GmbH" file="GetQueue.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
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