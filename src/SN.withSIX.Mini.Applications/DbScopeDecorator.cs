// <copyright company="SIX Networks GmbH" file="DbScopeDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications
{
    public class DbScopeDecorator : MediatorDecoratorBase
    {
        readonly IDbContextFactory _factory;

        public DbScopeDecorator(IMediator target, IDbContextFactory factory) : base(target) {
            _factory = factory;
        }

        public override TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            using (var scope = _factory.Create()) {
                TResponseData v;
                try {
                    v = base.Send(request);
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

        public override async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            using (var scope = _factory.Create()) {
                TResponseData r;
                try {
                    r = await base.SendAsync(request).ConfigureAwait(false);
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

        public override async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            using (var scope = _factory.Create()) {
                TResponse r;
                try {
                    r = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
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