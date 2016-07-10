// <copyright company="SIX Networks GmbH" file="RemoveQueueItem.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class RemoveQueueItem : IAsyncVoidCommand
    {
        public RemoveQueueItem(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class RemoveQueueItemHandler : DbRequestBase, IAsyncVoidCommandHandler<RemoveQueueItem>
    {
        private readonly IQueueManager _queueManager;

        public RemoveQueueItemHandler(IDbContextLocator dbContextLocator, IQueueManager queueManager)
            : base(dbContextLocator) {
            _queueManager = queueManager;
        }

        public Task<UnitType> HandleAsync(RemoveQueueItem request) => _queueManager.RemoveFromQueue(request.Id).Void();
    }
}