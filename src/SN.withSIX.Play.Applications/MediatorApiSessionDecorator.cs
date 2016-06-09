using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Play.Core.Connect.Infrastructure;

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

        public TResponseData Request<TResponseData>(IRequest<TResponseData> request) {
            if (request is IRequireApiSession) {
                using (var scope = _scopeFactory.StartSession().Result) {
                    var response = _mediator.Request(request);
                    scope.Close().WaitAndUnwrapException();
                    return response;
                }
            }
            return _mediator.Request(request);
        }

        public async Task<TResponseData> RequestAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            if (request is IRequireApiSession) {
                using (var scope = await _scopeFactory.StartSession().ConfigureAwait(false)) {
                    var response = await _mediator.RequestAsync(request).ConfigureAwait(false);
                    await scope.Close().ConfigureAwait(false);
                    return response;
                }
            }
            return await _mediator.RequestAsync(request).ConfigureAwait(false);
        }

        public void Notify<TNotification>(TNotification notification) {
            _mediator.Notify(notification);
        }

        public Task NotifyAsync<TNotification>(TNotification notification) => _mediator.NotifyAsync(notification);
    }

    public interface IRequireApiSession {}
}