// <copyright company="SIX Networks GmbH" file="DbScopeDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ShortBus;
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

        public TResponseData Request<TResponseData>(IRequest<TResponseData> request) {
            using (var scope = _factory.Create()) {
                TResponseData v;
                try {
                    v = _target.Request(request);
                    if (request is IWrite)
                        scope.SaveChanges();
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

        public async Task<TResponseData> RequestAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            using (var scope = _factory.Create()) {
                TResponseData r;
                try {
                    r = await _target.RequestAsync(request).ConfigureAwait(false);
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

        public void Notify<TNotification>(TNotification notification) => _target.Notify(notification);

        public Task NotifyAsync<TNotification>(TNotification notification) => _target.NotifyAsync(notification);
    }
}