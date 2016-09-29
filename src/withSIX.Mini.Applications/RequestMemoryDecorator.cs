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

        public override TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            try {
                return base.Send(request);
            } finally {
                Collect();
            }
        }

        public override async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            try {
                return await base.SendAsync(request).ConfigureAwait(false);
            } finally {
                Collect();
            }
        }

        public override async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            try {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
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