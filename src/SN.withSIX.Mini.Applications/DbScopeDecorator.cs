// <copyright company="SIX Networks GmbH" file="DbScopeDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications
{
    public class DbScopeDecorator : IMediator
    {
        readonly IDbContextFactory _factory;
        readonly IMediator _target;

        public DbScopeDecorator(IMediator target, IDbContextFactory factory) {
            _target = target;
            _factory = factory;
        }

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            using (var scope = _factory.Create()) {
                TResponseData v;
                try {
                    v = _target.Send(request);
                } catch (OperationCanceledException) {
                    // we still want to save on cancelling..
                    if (request is IWrite)
                        scope.SaveChanges();
                    throw;
                }
                if (request is IWrite)
                    scope.SaveChanges();
                return v;
            }
        }

        public async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            using (var scope = _factory.Create()) {
                TResponseData r;
                try {
                    r = await _target.SendAsync(request).ConfigureAwait(false);
                } catch (OperationCanceledException) {
                    // we still want to save on cancelling..
                    if (request is IWrite)
                        await scope.SaveChangesAsync().ConfigureAwait(false);
                    throw;
                }
                if (request is IWrite)
                    await scope.SaveChangesAsync().ConfigureAwait(false);
                return r;
            }
        }

        public void Publish(INotification notification) => _target.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _target.PublishAsync(notification);

        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken)
            => _target.PublishAsync(notification, cancellationToken);

        public async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request, CancellationToken cancellationToken) {
            using (var scope = _factory.Create()) {
                TResponse r;
                try {
                    r = await _target.SendAsync(request, cancellationToken).ConfigureAwait(false);
                } catch (OperationCanceledException) {
                    // we still want to save on cancelling..
                    if (request is IWrite)
                        await scope.SaveChangesAsync().ConfigureAwait(false);
                    throw;
                }
                if (request is IWrite)
                    await scope.SaveChangesAsync().ConfigureAwait(false);
                return r;
            }
        }
    }
}