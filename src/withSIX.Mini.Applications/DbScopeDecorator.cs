// <copyright company="SIX Networks GmbH" file="DbScopeDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications
{
    public class DbScopeDecorator : MediatorDecoratorBase
    {
        readonly IDbContextFactory _factory;

        public DbScopeDecorator(IMediator target, IDbContextFactory factory) : base(target) {
            _factory = factory;
        }

        public override async Task<TResponseData> Send<TResponseData>(IRequest<TResponseData> request,
            CancellationToken cancelToken = default(CancellationToken)) {
            using (var scope = _factory.Create()) {
                TResponseData r;
                try {
                    r = await base.Send(request, cancelToken).ConfigureAwait(false);
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

        public override async Task Send(IRequest request, CancellationToken cancelToken = default(CancellationToken)) {
            using (var scope = _factory.Create()) {
                try {
                    await base.Send(request, cancelToken).ConfigureAwait(false);
                } catch (OperationCanceledException) {
                    // we still want to save on cancelling..
                    if (request is IWrite)
                        await scope.SaveChangesAsync().ConfigureAwait(false);
                    throw;
                }
                if (request is IWrite)
                    await scope.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}