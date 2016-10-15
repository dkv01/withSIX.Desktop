// <copyright company="SIX Networks GmbH" file="Validation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;

namespace withSIX.Core.Presentation.Services
{
    public sealed class CompositeValidator<T> : AbstractValidator<T>
    {
        private readonly IValidator<T>[] _validators;

        public CompositeValidator(IEnumerable<IValidator<T>> validators) {
            _validators = validators.ToArray(); // let's keep them cached. Seeing as it's apparantly hard to make to configure them as singletons
        }

        public override ValidationResult Validate(ValidationContext<T> context) {
            var errorsFromOtherValidators = _validators.SelectMany(x => x.Validate(context).Errors);

            return new ValidationResult(errorsFromOtherValidators);
        }

        public override async Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken ct) {
            var errorsFromOtherValidators =
                await Task.WhenAll(_validators.Select(x => x.ValidateAsync(context, ct))).ConfigureAwait(false);
            return new ValidationResult(errorsFromOtherValidators.SelectMany(x => x.Errors));
        }
    }
}