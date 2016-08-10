// <copyright company="SIX Networks GmbH" file="ValidatingRequestDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace SN.withSIX.Core.Presentation.Decorators
{
    public class MediatorValidationDecorator : MediatorLoggingDecorator
    {
        readonly DataAnnotationsValidator.DataAnnotationsValidator _validator =
            new DataAnnotationsValidator.DataAnnotationsValidator();

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
}