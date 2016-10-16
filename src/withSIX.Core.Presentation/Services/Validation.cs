// <copyright company="SIX Networks GmbH" file="Validation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using withSIX.Core.Applications.Factories;
using withSIX.Core.Applications.Services;

namespace withSIX.Core.Presentation.Services
{
    public sealed class CompositeValidator<T> : AbstractValidator<T>
    {
        private readonly IValidator<T>[] _validators;

        public CompositeValidator(IEnumerable<IValidator<T>> validators) {
            _validators = validators.ToArray();
                // let's keep them cached. Seeing as it's apparantly hard to make to configure them as singletons
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

    public class PolymorphicValidator : NoopPropertyValidator, IPolymorphicValidator, IPresentationService
    {
        private readonly IValidatorResolver _resolver;

        public PolymorphicValidator(IValidatorResolver resolver) {
            _resolver = resolver;
        }

        public override async Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context,
            CancellationToken ct) {
            if (context.PropertyValue == null)
                return Enumerable.Empty<ValidationFailure>();
            var v = _resolver.GetValidatorForSpecial(context.PropertyValue);
            var r = await v.ValidateAsync(context.PropertyValue, ct).ConfigureAwait(false);
            return r.Errors;
        }

        public override IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context) {
            // bail out if the property is null 
            if (context.PropertyValue == null) return Enumerable.Empty<ValidationFailure>();
            var v = _resolver.GetValidatorForSpecial(context.PropertyValue);
            return v.Validate(context.PropertyValue).Errors;


            /*
            // Make sure property is of correct type
            IEnumerable<TBaseClass> collection = context.PropertyValue as IEnumerable<TBaseClass>;
            if (collection == null) return Enumerable.Empty<ValidationFailure>();

            var results = new List<ValidationFailure>();

            int index = 0;
            foreach (TBaseClass item in collection) {
                IValidator derivedValidator;

                // Find the validator to use based on actual type of the element.
                var actualType = item.GetType();
                if (derivedValidators.TryGetValue(actualType, out derivedValidator)) {
                    var newContext = context.ParentContext.Clone(instanceToValidate: item);
                    newContext.PropertyChain.Add(context.Rule.PropertyName);
                    newContext.PropertyChain.AddIndexer(index);

                    // Execute child validator. 
                    results.AddRange(derivedValidator.Validate(newContext).Errors);
                }

                index++;
            }

            return results;
            */
        }
    }

    // Used within validators to validate child objets which might be polymoprhic
    public class ValidatorResolver : IValidatorResolver, IPresentationService
    {
        private readonly IDepResolver _resolver;

        public ValidatorResolver(IDepResolver resolver) {
            _resolver = resolver;
        }

        public IValidator GetValidatorForSpecial(object obj) => GetValidatorFor((dynamic) obj);
        public IValidator<T> GetValidatorFor<T>(T obj) => _resolver.GetInstance<IValidator<T>>();
    }
}