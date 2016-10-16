using FluentValidation;
using FluentValidation.Validators;

namespace withSIX.Core.Applications.Services
{
    public interface IValidatorResolver {
        IValidator GetValidatorForSpecial(object obj);
    }

    public interface IPolymorphicValidator : IPropertyValidator { }

}