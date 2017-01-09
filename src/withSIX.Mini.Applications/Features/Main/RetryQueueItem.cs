// <copyright company="SIX Networks GmbH" file="RetryQueueItem.cs">
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
    public class RetryQueueItem : ICommand
    {
        public RetryQueueItem(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class RetryQueueItemHandler : DbRequestBase, IAsyncRequestHandler<RetryQueueItem>
    {
        private readonly IQueueManager _queueManager;

        public RetryQueueItemHandler(IDbContextLocator dbContextLocator, IQueueManager queueManager)
            : base(dbContextLocator) {
            _queueManager = queueManager;
        }

        public Task Handle(RetryQueueItem request) => _queueManager.Retry(request.Id).Void();
    }
}