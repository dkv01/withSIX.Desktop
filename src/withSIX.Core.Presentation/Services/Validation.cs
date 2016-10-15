// <copyright company="SIX Networks GmbH" file="Validation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;

namespace withSIX.Core.Presentation.Services
{
    public abstract class CompositeValidator<T> : AbstractValidator<T>
    {
        private readonly IEnumerable<IValidator<T>> _validators;

        public CompositeValidator(IEnumerable<IValidator<T>> validators) {
            _validators = validators;
        }

        public override ValidationResult Validate(ValidationContext<T> context) {
            var errorsFromOtherValidators = _validators.SelectMany(x => x.Validate(context).Errors);

            return new ValidationResult(errorsFromOtherValidators);
        }
    }

    public class EmptyValidator<T> : AbstractValidator<T> {}
}