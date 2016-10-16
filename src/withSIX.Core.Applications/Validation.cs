// <copyright company="SIX Networks GmbH" file="Validation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using FluentValidation;
using withSIX.Api.Models.Content.v3;

namespace withSIX.Core.Applications
{
    public class IdValidator : AbstractValidator<IHaveId<Guid>>
    {
        public IdValidator() {
            RuleFor(x => x.Id)
                .NotEqual(Guid.Empty);
        }
    }
}