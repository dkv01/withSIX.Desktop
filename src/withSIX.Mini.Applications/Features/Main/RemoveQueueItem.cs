// <copyright company="SIX Networks GmbH" file="RemoveQueueItem.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    public class RemoveQueueItem : IVoidCommand
    {
        public RemoveQueueItem(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class RemoveQueueItemHandler : DbRequestBase, IAsyncRequestHandler<RemoveQueueItem>
    {
        private readonly IQueueManager _queueManager;

        public RemoveQueueItemHandler(IDbContextLocator dbContextLocator, IQueueManager queueManager)
            : base(dbContextLocator) {
            _queueManager = queueManager;
        }

        public Task Handle(RemoveQueueItem request) => _queueManager.RemoveFromQueue(request.Id).Void();
    }
}