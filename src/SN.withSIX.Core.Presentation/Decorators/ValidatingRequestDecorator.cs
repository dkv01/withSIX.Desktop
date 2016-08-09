// <copyright company="SIX Networks GmbH" file="ValidatingRequestDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace SN.withSIX.Core.Presentation.Decorators
{
    public abstract class BaseValidator
    {
        readonly DataAnnotationsValidator.DataAnnotationsValidator _validator =
            new DataAnnotationsValidator.DataAnnotationsValidator();

        [DebuggerStepThrough]
        protected void Validate(object o) {
            //var validationContext = new ValidationContext(query, this.provider, null);
            //Validator.ValidateObject(o, new ValidationContext(query), true); // , validationContext
            _validator.ValidateObject(o); // , validationContext
        }
    }

    public class MediatorValidationDecorator : BaseValidator, IMediator
    {
        readonly IMediator _mediator;

        public MediatorValidationDecorator(IMediator mediator) {
            Contract.Requires<ArgumentNullException>(mediator != null);
            _mediator = mediator;
        }

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            Validate(request);
            return _mediator.Send(request);
        }

        public Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            Validate(request);
            return _mediator.SendAsync(request);
        }

        public void Publish(INotification notification) => _mediator.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _mediator.PublishAsync(notification);

        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken)
            => _mediator.PublishAsync(notification, cancellationToken);

        public Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            Validate(request);
            return _mediator.SendAsync(request, cancellationToken);
        }
    }
}