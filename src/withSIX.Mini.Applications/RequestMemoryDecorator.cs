// <copyright company="SIX Networks GmbH" file="RequestMemoryDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Extensions;

namespace withSIX.Mini.Applications
{
    public class RequestMemoryDecorator : MediatorDecoratorBase
    {
        public RequestMemoryDecorator(IMediator target) : base(target) {}

        public override async Task<TResponseData> Send<TResponseData>(IRequest<TResponseData> request,
            CancellationToken cancelToken = default(CancellationToken)) {
            try {
                return await base.Send(request, cancelToken).ConfigureAwait(false);
            } finally {
                Collect();
            }
        }

        public override async Task Send(IRequest request, CancellationToken cancelToken = default(CancellationToken)) {
            try {
                await base.Send(request, cancelToken).ConfigureAwait(false);
            } finally {
                Collect();
            }
        }

        static void Collect() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}