// <copyright company="SIX Networks GmbH" file="MediatorDecoratorBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace withSIX.Core.Applications.Extensions
{
    // TODO: IWrappedMediator that adds contracts?
    public abstract class MediatorDecoratorBase : DecoratorBase<IMediator>, IMediator
    {
        protected MediatorDecoratorBase(IMediator decorated) : base(decorated) {}
        public virtual Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancelToken = default(CancellationToken)) => Decorated.Send(request, cancelToken);
        public virtual Task Send(IRequest request, CancellationToken cancelToken = default(CancellationToken)) => Decorated.Send(request, cancelToken);

        public Task Publish<TNotification>(TNotification notification,
            CancellationToken cancelToken = default(CancellationToken)) where TNotification : INotification
            => Decorated.Publish(notification, cancelToken);
    }
}