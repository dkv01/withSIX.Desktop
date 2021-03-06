﻿// <copyright company="SIX Networks GmbH" file="ValidatingRequestDecorator.cs">
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

        public override Task<TResponseData> Send<TResponseData>(IRequest<TResponseData> request, CancellationToken cancelToken = default(CancellationToken)) {
            Validate(request);
            return base.Send(request, cancelToken);
        }

        public override Task Send(IRequest request, CancellationToken cancelToken = default(CancellationToken))
        {
            Validate(request);
            return base.Send(request, cancelToken);
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
        internal static void ConfirmValidation<TRequest>(this TRequest request, IValidator<TRequest> validator)
            => validator.Validate(request).ConfirmValidationResult();

        [DebuggerStepThrough]
        internal static async Task ConfirmValidationAsync<TRequest>(this TRequest request,
                IValidator<TRequest> validator)
            => (await validator.ValidateAsync(request).ConfigureAwait(false)).ConfirmValidationResult();

        [DebuggerStepThrough]
        internal static async Task ConfirmValidationAsync<TRequest>(this TRequest request,
                IValidator<TRequest> validator, CancellationToken ct)
            => (await validator.ValidateAsync(request, ct).ConfigureAwait(false)).ConfirmValidationResult();
    }

    public class ValidationRequestHandler<TRequest> : IRequestHandler<TRequest>
        where TRequest : IRequest
    {
        private readonly IRequestHandler<TRequest> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationRequestHandler(IRequestHandler<TRequest> innerHandler,
            IValidator<TRequest> validator)
        {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public void Handle(TRequest message)
        {
            message.ConfirmValidation(_validator);

            _innerHander.Handle(message);
        }
    }

    public class ValidationRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationRequestHandler(IRequestHandler<TRequest, TResponse> innerHandler,
            IValidator<TRequest> validator)
        {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public TResponse Handle(TRequest message)
        {
            message.ConfirmValidation(_validator);

            return _innerHander.Handle(message);
        }
    }

    public class ValidationAsyncRequestHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationAsyncRequestHandler(IAsyncRequestHandler<TRequest, TResponse> innerHandler,
            IValidator<TRequest> validator)
        {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public async Task<TResponse> Handle(TRequest message)
        {
            await message.ConfirmValidationAsync(_validator).ConfigureAwait(false);
            return await _innerHander.Handle(message).ConfigureAwait(false);
        }
    }

    public class ValidationAsyncRequestHandler<TRequest> : IAsyncRequestHandler<TRequest>
    where TRequest : IRequest
    {
        private readonly IAsyncRequestHandler<TRequest> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationAsyncRequestHandler(IAsyncRequestHandler<TRequest> innerHandler,
            IValidator<TRequest> validator)
        {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public async Task Handle(TRequest message)
        {
            await message.ConfirmValidationAsync(_validator).ConfigureAwait(false);
            await _innerHander.Handle(message).ConfigureAwait(false);
        }
    }

    public class ValidationCancellableAsyncRequestHandler<TRequest, TResponse> :
        ICancellableAsyncRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly ICancellableAsyncRequestHandler<TRequest, TResponse> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationCancellableAsyncRequestHandler(
            ICancellableAsyncRequestHandler<TRequest, TResponse> innerHandler, IValidator<TRequest> validator)
        {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public async Task<TResponse> Handle(TRequest message, CancellationToken ct)
        {
            await message.ConfirmValidationAsync(_validator, ct).ConfigureAwait(false);
            return await _innerHander.Handle(message, ct).ConfigureAwait(false);
        }
    }

    public class ValidationCancellableAsyncRequestHandler<TRequest> :
        ICancellableAsyncRequestHandler<TRequest> where TRequest : IRequest
    {
        private readonly ICancellableAsyncRequestHandler<TRequest> _innerHander;
        private readonly IValidator<TRequest> _validator;

        public ValidationCancellableAsyncRequestHandler(
            ICancellableAsyncRequestHandler<TRequest> innerHandler, IValidator<TRequest> validator)
        {
            _validator = validator;
            _innerHander = innerHandler;
        }

        public async Task Handle(TRequest message, CancellationToken ct)
        {
            await message.ConfirmValidationAsync(_validator, ct).ConfigureAwait(false);
            await _innerHander.Handle(message, ct).ConfigureAwait(false);
        }
    }
}