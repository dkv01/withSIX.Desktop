// <copyright company="SIX Networks GmbH" file="MediatorDecoratorBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace withSIX.Core.Applications.Extensions
{
    public abstract class MediatorDecoratorBase : DecoratorBase<IMediator>, IMediator
    {
        protected MediatorDecoratorBase(IMediator decorated) : base(decorated) {}
        public virtual TResponse Send<TResponse>(IRequest<TResponse> request) => Decorated.Send(request);

        public virtual Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> request)
            => Decorated.SendAsync(request);

        public void Publish(INotification notification) => Decorated.Publish(notification);

        public virtual Task PublishAsync(IAsyncNotification notification) => Decorated.PublishAsync(notification);

        public virtual Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken)
            => Decorated.PublishAsync(notification, cancellationToken);

        public virtual Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) => Decorated.SendAsync(request, cancellationToken);
    }
}