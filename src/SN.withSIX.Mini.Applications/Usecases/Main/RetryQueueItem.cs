// <copyright company="SIX Networks GmbH" file="RetryQueueItem.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class RetryQueueItem : IAsyncVoidCommand
    {
        public RetryQueueItem(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class RetryQueueItemHandler : DbRequestBase, IAsyncVoidCommandHandler<RetryQueueItem>
    {
        private readonly IQueueManager _queueManager;

        public RetryQueueItemHandler(IDbContextLocator dbContextLocator, IQueueManager queueManager)
            : base(dbContextLocator) {
            _queueManager = queueManager;
        }

        public Task<Unit> Handle(RetryQueueItem request) => _queueManager.Retry(request.Id).Void();
    }
}