using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Play.Applications
{
    public class MediatorApiContextDecorator : IMediator
    {
        readonly IMediator _mediator;
        readonly IConnectApiHandler _scopeFactory;

        public MediatorApiContextDecorator(IMediator mediator, IConnectApiHandler scopeFactory) {
            Contract.Requires<ArgumentNullException>(mediator != null);
            Contract.Requires<ArgumentNullException>(scopeFactory != null);

            _mediator = mediator;
            _scopeFactory = scopeFactory;
        }

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            if (request is IRequireApiSession) {
                using (var scope = _scopeFactory.StartSession().Result) {
                    var response = _mediator.Send(request);
                    scope.Close().WaitAndUnwrapException();
                    return response;
                }
            }
            return _mediator.Send(request);
        }

        public async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            if (request is IRequireApiSession) {
                using (var scope = await _scopeFactory.StartSession().ConfigureAwait(false)) {
                    var response = await _mediator.SendAsync(request).ConfigureAwait(false);
                    await scope.Close().ConfigureAwait(false);
                    return response;
                }
            }
            return await _mediator.SendAsync(request).ConfigureAwait(false);
        }

        public void Publish(INotification notification) => _mediator.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _mediator.PublishAsync(notification);

        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken) => _mediator.PublishAsync(notification, cancellationToken);

        public async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request, CancellationToken cancellationToken) {
            if (request is IRequireApiSession) {
                using (var scope = await _scopeFactory.StartSession().ConfigureAwait(false)) {
                    var response = await _mediator.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    await scope.Close().ConfigureAwait(false);
                    return response;
                }
            }
            return await _mediator.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    public interface IRequireApiSession {}
}