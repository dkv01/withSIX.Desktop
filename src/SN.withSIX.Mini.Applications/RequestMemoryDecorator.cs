// <copyright company="SIX Networks GmbH" file="RequestMemoryDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace SN.withSIX.Mini.Applications
{
    public class RequestMemoryDecorator : IMediator
    {
        readonly IMediator _target;

        public RequestMemoryDecorator(IMediator target) {
            _target = target;
        }

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            try {
                return _target.Send(request);
            } finally {
                Collect();
            }
        }

        public async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            try {
                return await _target.SendAsync(request).ConfigureAwait(false);
            } finally {
                Collect();
            }
        }

        public void Publish(INotification notification) => _target.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _target.PublishAsync(notification);

        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken)
            => _target.PublishAsync(notification, cancellationToken);

        public async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            try {
                return await _target.SendAsync(request, cancellationToken).ConfigureAwait(false);
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