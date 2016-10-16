// <copyright company="SIX Networks GmbH" file="IValidatorResolver.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using FluentValidation;
using FluentValidation.Validators;

namespace withSIX.Core.Applications.Services
{
    public interface IValidatorResolver
    {
        IValidator GetValidatorForSpecial(object obj);
    }

    public interface IPolymorphicValidator : IPropertyValidator {}

    public class ValidPathValidator : PropertyValidator
    {
        public ValidPathValidator()
            : base("Property {PropertyName} is not a valid absolute path!") {}

        protected override bool IsValid(PropertyValidatorContext context) {
            var p = context.PropertyValue as string;
            return (p == null) || Tools.FileUtil.IsValidRootedPath(p);
        }
    }
}