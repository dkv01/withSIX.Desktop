// <copyright company="SIX Networks GmbH" file="CancelQueueItem.cs">
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
    public class CancelQueueItem : IAsyncVoidCommand
    {
        public CancelQueueItem(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class CancelQueueItemByContentId : IAsyncVoidCommand
    {
        public CancelQueueItemByContentId(Guid contentId) {
            ContentId = contentId;
        }

        public Guid ContentId { get; }
    }

    public class CancelQueueItemHandler : DbRequestBase, IAsyncRequestHandler<CancelQueueItem>, IAsyncRequestHandler<CancelQueueItemByContentId>
    {
        private readonly IQueueManager _queueManager;

        public CancelQueueItemHandler(IDbContextLocator dbContextLocator, IQueueManager queueManager)
            : base(dbContextLocator) {
            _queueManager = queueManager;
        }

        public Task Handle(CancelQueueItem request) => _queueManager.Cancel(request.Id).Void();
        public Task Handle(CancelQueueItemByContentId request) => _queueManager.CancelByContentId(request.ContentId).Void();
    }
}