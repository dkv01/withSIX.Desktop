// <copyright company="SIX Networks GmbH" file="ValidatingRequestDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using withSIX.Api.Models.Validators;
using withSIX.Core.Applications.Extensions;

namespace withSIX.Core.Presentation.Decorators
{
    public class MediatorValidationDecorator : MediatorDecoratorBase
    {
        readonly DataAnnotationsValidator _validator = new DataAnnotationsValidator();

        public MediatorValidationDecorator(IMediator decorated) : base(decorated) {}


        public override TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            Validate(request);
            return base.Send(request);
        }

        public override Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            Validate(request);
            return base.SendAsync(request);
        }

        public override Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            Validate(request);
            return base.SendAsync(request, cancellationToken);
        }

        [DebuggerStepThrough]
        protected void Validate(object o) {
            //var validationContext = new ValidationContext(query, this.provider, null);
            //Validator.ValidateObject(o, new ValidationContext(query), true); // , validationContext
            _validator.ValidateObject(o); // , validationContext
        }
    }

    internal static class ValidationExt
    {
        [DebuggerStepThrough]
        internal static void ConfirmValidationResult(this ValidationResult result) {
            try {
                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            } catch (ValidationException ex) {
                // TODO: Be nice
                throw new Api.Models.Exceptions.ValidationException(ex.Message, ex);
            }
        }

        [DebuggerStepThrough]
        internal static ValidationResult Validate<TRequest>(this TRequest request, IValidator<TRequest> validator) {
            var context = new ValidationContext<TRequest>(request);
            return validator.Validate(context);
        }

        [DebuggerStepThrough]
        internal static Task<ValidationResult> ValidateAsync<TRequest>(this TRequest request,
            IValidator<TRequest> validator) {
            var context = new ValidationContext<TRequest>(request);
            return validator.ValidateAsync(context);
        }

        [DebuggerStepThrough]
        internal static void ConfirmValidation<TRequest>(this TRequest request, IValidator<TRequest> validator)
            => request.Validate(validator).ConfirmValidationResult();

        [DebuggerStepThrough]
        internal static async Task ConfirmValidationAsync<TRequest>(this TRequest request,
                IValidator<TRequest> validator)
            => (await request.ValidateAsync(validator).ConfigureAwait(false)).ConfirmValidationResult();
    }

    public class ValidationRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationRequestHandler(IRequestHandler<TRequest, TResponse> innerHandler,
            IValidator<TRequest> validator) {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public TResponse Handle(TRequest message) {
            message.ConfirmValidation(_validator);

            return _innerHander.Handle(message);
        }
    }

    public class ValidationAsyncRequestHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
        where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationAsyncRequestHandler(IAsyncRequestHandler<TRequest, TResponse> innerHandler,
            IValidator<TRequest> validator) {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public async Task<TResponse> Handle(TRequest message) {
            await message.ConfirmValidationAsync(_validator).ConfigureAwait(false);
            return await _innerHander.Handle(message).ConfigureAwait(false);
        }
    }

    public class ValidationCancellableAsyncRequestHandler<TRequest, TResponse> :
        ICancellableAsyncRequestHandler<TRequest, TResponse> where TRequest : ICancellableAsyncRequest<TResponse>
    {
        private readonly ICancellableAsyncRequestHandler<TRequest, TResponse> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationCancellableAsyncRequestHandler(
            ICancellableAsyncRequestHandler<TRequest, TResponse> innerHandler, IValidator<TRequest> validator) {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public async Task<TResponse> Handle(TRequest message, CancellationToken ct) {
            await message.ConfirmValidationAsync(_validator).ConfigureAwait(false);
            return await _innerHander.Handle(message, ct).ConfigureAwait(false);
        }
    }
}