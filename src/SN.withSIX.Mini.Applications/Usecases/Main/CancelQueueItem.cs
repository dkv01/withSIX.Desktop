// <copyright company="SIX Networks GmbH" file="CancelQueueItem.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class CancelQueueItem : IAsyncVoidCommand
    {
        public CancelQueueItem(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }


    public class CancelQueueItemHandler : DbRequestBase, IAsyncVoidCommandHandler<CancelQueueItem>
    {
        private readonly IQueueManager _queueManager;

        public CancelQueueItemHandler(IDbContextLocator dbContextLocator, IQueueManager queueManager)
            : base(dbContextLocator) {
            _queueManager = queueManager;
        }

        public Task<Unit> Handle(CancelQueueItem request) => _queueManager.Cancel(request.Id).Void();
    }
}